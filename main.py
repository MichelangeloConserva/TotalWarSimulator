import random
import numpy as np
import imageio

import pygame
import pymunk
import pymunk.pygame_util

from pymunk.vec2d import Vec2d
from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_q, MOUSEBUTTONDOWN, MOUSEBUTTONUP
from pymunk.pygame_util import from_pygame, to_pygame
from time import time
from io import BytesIO
from PIL import Image
from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment

# from characters import Army
from utils import create_space, is_on_enemy_unity
from characters import InfantryUnit, Army


WIDTH, HEIGHT = 500, 300
UNIT_SIZE = 10
RED = (255, 0, 0)
GREEN = (0, 255, 0)
GHOST_RED = (255, 0, 0, 100)
DARKGREEN = pygame.color.THECOLORS["darkgreen"]
LEFT = 1
RIGHT = 3

def vector_clipping(v, min_length, max_length):
    if v.length > max_length:
        return v.normalized() * max_length
    elif v.length < min_length:
        return v.normalized() * min_length        
    return v


def vect_linspace(v1, v2, n):
    prs = np.linspace(0,1, n+1).tolist()            
    res = []
    for i in range(n):
        pr_start = prs[i]
        pr_end = prs[i+1]
        cur_start = v1*(pr_start) + (v2)*(1-pr_start)
        cur_end = v1*(pr_end) + (v2)*(1-pr_end)
        res.append((cur_start, cur_end))
    return res
        
def vect_middles_of_linspace(v1,v2,n):
    divs = np.linspace(0,1, n+2).tolist()[1:-1]
    return np.array([(v1 * div + v2 * (1 - div)).int_tuple for div in divs])


def get_BB(v1, v2):
    v1,v2 = Vec2d(v1), Vec2d(v2)
    left = min(v1.x, v2.x) #+ abs(v1.x - v2.x) / 2
    bot  = min(v1.y, v2.y) #- abs(v1.y - v2.y) / 2
    right = max(v1.x, v2.x) #+ abs(v1.x - v2.x) / 2
    top  = max(v1.y, v2.y) #- abs(v1.y - v2.y) / 2
    return pymunk.BB(left, bot, right, top)


class Game:
    
    def __init__(self, u_att = (2,0,0), u_def = (1,0,0), record = True):

        self.objects = []
        self.video = []
        self.fps = 60.0 
        self.done = False
        self.record = record
    
        pygame.init()
        
        InfantryUnit.font = pygame.font.Font(pygame.font.get_default_font(), 8)
        
        
        self.screen = pygame.display.set_mode((WIDTH,HEIGHT)) 
        self.space = create_space(WIDTH, HEIGHT)
        
        self.space.add_collision_handler(2, 1).pre_solve = self.enemy_coll
        self.space.add_collision_handler(1, 1).pre_solve = self.allied_coll
        self.space.add_collision_handler(2, 2).pre_solve = self.allied_coll
        
        self.clock = pygame.time.Clock()
        # self.draw_options = pymunk.pygame_util.DrawOptions(self.screen)
        
        self.initiate_armies(u_att, u_def)
    
    def enemy_coll(self, arbiter, space, _):
        att_unit, def_unit = arbiter.shapes
        
        att_unit.body.unit.fighting_against.add(def_unit.body.unit)
        def_unit.body.unit.fighting_against.add(att_unit.body.unit)        
        
        return True
        
    def allied_coll(self, arbiter, space, _): return False
    
    
    
    def update(self, dt):
        self.space.step(dt)
        self.attacker.update(dt)
        self.defender.update(dt)
    
    
    def initiate_armies(self, u_att, u_def):
        self.attacker = Army(self, (WIDTH/2, HEIGHT/6), WIDTH, HEIGHT, 
                              units = u_att)
        self.defender = Army(self, (WIDTH/2, 5*HEIGHT/6), WIDTH, HEIGHT, 
                              units = u_def, role = "Defender")
        self.attacker.enemy, self.defender.enemy = self.defender, self.attacker
        
        
    def on_mouse_down(self, event):
        
        if event.button == LEFT and not self.drag_lmb:
            self.start_pos_lmb = event.pos
            self.drag_lmb = True
        
        elif event.button == RIGHT:
            self.start_pos_rmb = from_pygame(event.pos, self.screen)
            
            for u in self.defender.units:
                if u.shape.bb.contains_vect(Vec2d(self.start_pos_rmb)):
                    for au in self.selected_units: 
                        au.set_dest(u.body.position, (u.body.position - au.body.position).angle)
                        return
            self.drag_rmb = True    
                
                
    def on_mouse_up(self, event):
        
        
        if event.button == LEFT:
            if self.drag_lmb:
                self.end_pos = from_pygame(event.pos, self.screen)
                
                # Selecting units that are inside the rectangle selection
                
                # self.selected_units = set()   We might want to empty this
                bb = selection_BB = get_BB(from_pygame(self.start_pos_lmb, self.screen), 
                                           self.end_pos)
                
                pos = bb.left, bb.top
                w = bb.right - bb.left
                h = bb.top - bb.bottom
                p = to_pygame(pos, self.screen)
                pygame.draw.rect(self.screen, DARKGREEN, (*p, w, h), 5)                    
                
                selection = False
                for u in self.attacker.units:
                    if u.shape.bb.intersects(selection_BB):
                        u.is_selected = True
                        self.selected_units.add(u)
                        selection = True
                self.drag_lmb = False
                
                # Deselecting if no units was in the selection rectangle
                if not selection:
                    for u in self.attacker.units: 
                        u.is_selected = False
                    self.selected_units = set()                
                
                
        elif event.button == RIGHT:
            start_pos_rmb = Vec2d(self.start_pos_rmb)
            end_pos_rmb = Vec2d(from_pygame(event.pos, self.screen))
            dd = (start_pos_rmb-end_pos_rmb).get_length_sqrd()
            
            # TODO : implement move_units_with_formation even in one click case
            
            if  dd < 30**2: # Just one click
                
                total_w = 0
                units_pos = Vec2d((0,0))
                for u in self.selected_units:
                    total_w += u.w + u.dist
                    units_pos += u.pos / len(self.selected_units)
            
                front_line = (end_pos_rmb - units_pos).perpendicular_normal()
                
                
                start_pos = end_pos_rmb + front_line * total_w / 2 
                end_pos = end_pos_rmb - front_line * total_w / 2 
                
                start_pos = Vec2d(to_pygame(start_pos, self.screen))
                end_pos = Vec2d(to_pygame(end_pos, self.screen))                
            
                # for u in self.selected_units:
                #     print(u.width_size)
            
                # for u in self.selected_units: u.dest = self.start_pos_rmb
            else:           # Moving with the formation
                start_pos = Vec2d(to_pygame(start_pos_rmb, self.screen))
                end_pos = Vec2d(to_pygame(end_pos_rmb, self.screen))
            
            self.move_units_with_formation(start_pos, end_pos, send_command = True)
            self.drag_rmb = False
    
    
    def draw(self):
        if self.drag_rmb:
            # Drawing the ghost of the formation
                self.move_units_with_formation(Vec2d(to_pygame(self.start_pos_rmb, self.screen)),
                                               Vec2d(pygame.mouse.get_pos()),
                                               send_command = False)
        
        if self.drag_lmb:
            # Drawing the ghost of the selection
            mp = pygame.mouse.get_pos()
            if sum((np.array(self.start_pos_lmb)-np.array(mp))**2) > 30**2:
                point_list = [self.start_pos_lmb, (self.start_pos_lmb[0], mp[1]), mp, (mp[0], self.start_pos_lmb[1])]
                pygame.draw.polygon(self.screen, RED,  point_list, 3)    
        
        # Drawing pymunk objects
        # self.space.debug_draw(self.draw_options)
        for o in self.objects: o.draw()
    
    
    def move_units_with_formation(self, start_pos, end_pos, send_command = False):

        if len(self.selected_units) != 0:
            
            selected_units = list(self.selected_units).copy()
            diff_vect_all = start_pos-end_pos
            
            # diff_vect_all clipping
            max_row_l = min_row_l = 0
            for u in self.selected_units:
                max_row_l += u.max_w  + u.dist
                min_row_l += u.min_w  + u.dist
                
            diff_vect_all = vector_clipping(diff_vect_all, min_row_l, max_row_l)
            perp = diff_vect_all.perpendicular_normal()
            
            # Find the minimal distance assignment of the unit to the proportions
            # only necessary if we are issueing the command
            
    
            units = list(selected_units)
            
            # Find middle points in the proportions
            divs = vect_middles_of_linspace(start_pos,start_pos - diff_vect_all,len(units))
            
            units_pos = np.array([np.array(u.pos.int_tuple) for u in units])

            pd = pairwise_distances(units_pos, divs)
            row_ind, col_ind = linear_sum_assignment(pd) 
            
            if send_command:
                for ri,ci in zip(row_ind,col_ind): 
                    units[ri].set_dest(to_pygame(divs[ci].tolist(), self.screen), 
                                       -perp.angle)
            
            start_ends = vect_linspace(start_pos, start_pos - diff_vect_all, len(self.selected_units))
            for (cur_start, cur_end),u in zip(start_ends, selected_units):        
                
                diff = cur_end - cur_start
                diff = diff.normalized() * (diff.length - u.dist)
                l = diff.length
                
                if l != 0:
                    w = l
                    h = u.shape.area / l

                    vs = [cur_start, 
                          cur_start + diff, 
                          cur_start + diff - perp*h, 
                          cur_start - perp*h]
                    pygame.draw.polygon(self.screen, GHOST_RED, vs)
                    
                    if send_command: 
                        u.w = w; u.h = h
                        
    
    
    def run(self):
            
        self.selected_units = set()
        self.drag_lmb = self.drag_rmb = False
        
        while True:
            self.screen.fill(pygame.color.THECOLORS["white"])
            
            for event in pygame.event.get():
                
                if event.type == QUIT or \
                    event.type == KEYDOWN and (event.key in [K_ESCAPE, K_q]): 
                    pygame.quit()
                    self.done = True
                    # self.save_video()   # might take some time to run
                    return
            
                elif event.type == MOUSEBUTTONDOWN: self.on_mouse_down(event)
                elif event.type == MOUSEBUTTONUP:   self.on_mouse_up(event)
        
            
        
            self.update(1/self.fps)
            self.draw()
            
            pygame.display.flip()
            self.clock.tick(self.fps)
                        
            if self.record:
                arr = pygame.surfarray.array3d(self.screen).swapaxes(1, 0)
                self.video.append(arr.astype(np.uint8))
        
    def save_video(self): imageio.mimwrite('test.gif', self.video , fps = self.fps)




if __name__ == "__main__":
    game = Game()
    game.run()
    
    if False:
        pygame.quit()
