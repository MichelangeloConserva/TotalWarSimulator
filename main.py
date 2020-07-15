"""
https://github.com/viblo/pymunk/blob/master/examples/tank.py
"""

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

from characters import Army
from utils import create_space, is_in_selection, is_on_enemy_unity

WIDTH, HEIGHT = 800, 600
UNIT_SIZE = 10
RED = (255, 0, 0)
GREEN = (0, 255, 0)
GHOST_RED = (255, 0, 0, 100)
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


def get_formation_from_front_line(v1, front_line, u, screen, send_command):

    formation = []
    perp = front_line.perpendicular_normal()
    u_size, u_dist = u.soldier_size_dist
    upr = np.minimum(int((front_line).length / (u_size + u_dist)),u.n//2)
    alphas = np.linspace(0,1, upr).tolist()[:-1]
    
    if len(alphas) > 0:
        n_rows_with_additional = u.n - len(alphas)*(u.n // len(alphas))

        if send_command:        
            u.width = len(alphas)
            u.height = u.n // len(alphas) + (1 if n_rows_with_additional != 0 else 0)
        
        
    for a in alphas:
        for r in range(u.n // len(alphas)):
            pos = Vec2d((v1*(a) + (v1 - front_line)*(1-a) +\
                         perp * (r * (u_size + u_dist)) ).int_tuple)
                
            # TODO : this draw should not be here
            pygame.draw.circle(screen, GHOST_RED, pos , u_size//2)
            
            if send_command: formation.append(to_pygame(pos, screen))
            
        if n_rows_with_additional > 0:
            pos = Vec2d((v1*(a) + (v1 - front_line)*(1-a) +\
                         perp * ((r+1) * (u_size + u_dist)) ).int_tuple)
            pygame.draw.circle(screen, GHOST_RED, pos , u_size//2)
            n_rows_with_additional -= 1
        
            if send_command: formation.append(to_pygame(pos, screen))



    if send_command: return formation



class Game:
    
    def __init__(self, u_att = (2,0,0), u_def = (1,0,0), record = True):

        self.video = []
        self.fps = 60.0 
        self.done = False
        self.record = record
    
        pygame.init()
        self.screen = pygame.display.set_mode((WIDTH,HEIGHT)) 
        self.space = create_space(WIDTH, HEIGHT)
        
        self.clock = pygame.time.Clock()
        self.draw_options = pymunk.pygame_util.DrawOptions(self.screen)
        
        self.initiate_armies(u_att, u_def)
        
    
    def update(self, dt):
        self.space.step(dt)
        self.attacker.update(dt)
        self.defender.update(dt)
    
    
    def initiate_armies(self, u_att, u_def):
        self.attacker = Army(self.space, (WIDTH/2, HEIGHT/6), WIDTH, HEIGHT, 
                             units = u_att)
        self.defender = Army(self.space, (WIDTH/2, 5*HEIGHT/6), WIDTH, HEIGHT, 
                             units = u_def, role = "Defender")
        self.attacker.enemy, self.defender.enemy = self.defender, self.attacker
        self.attacker.screen = self.defender.screen = self.screen
        
        
    def on_mouse_down(self, event):
        
        if event.button == LEFT and not self.drag_lmb:
            self.start_pos_lmb = event.pos
            self.drag_lmb = True
        
        elif event.button == RIGHT:
            self.start_pos_rmb = from_pygame(event.pos, self.screen)
            
            enemy_unit = is_on_enemy_unity(self.start_pos_rmb, self.attacker)
            if not enemy_unit is None:
                for u in self.selected_units: u.attack(enemy_unit)
            else:
                self.drag_rmb = True    
                
                
    def on_mouse_up(self, event):
        
        
        if event.button == LEFT:
            if self.drag_lmb:
                self.end_pos = from_pygame(event.pos, self.screen)
                
                # Selecting units that are inside the rectangle selection
                
                # self.selected_units = set()   We might want to empty this
                selection = False
                for u in self.attacker.units:
                    for s in u.soldiers:
                        start_pos = from_pygame(self.start_pos_lmb, self.screen)
                        pos = list(s.body.position)
                        if is_in_selection(pos, start_pos, self.end_pos): 
                            u.is_selected = selection = True
                            self.selected_units.add(u)
                            break
                self.drag_lmb = False
                
                # Deselecting if no units was in the selection rectangle
                if not selection:
                    for u in self.attacker.units: u.is_selected = False
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
                    total_w += u.width_size + 2*sum(u.soldier_size_dist)
                    units_pos += u.pos / len(self.selected_units)
            
                front_line = (end_pos_rmb - units_pos).perpendicular_normal()
                
                print(total_w)
                
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
        self.space.debug_draw(self.draw_options)
    
    
    
    def move_units_with_formation(self, start_pos, end_pos, send_command = False):

        if len(self.selected_units) != 0:
            
            selected_units = list(self.selected_units).copy()
            diff_vect_all = start_pos-end_pos
            
            # diff_vect_all clipping
            max_row_l = min_row_l = 0
            for u in self.selected_units:
                u_size, u_dist = u.soldier_size_dist
                max_row_l += u_size * u.n//2 + u_dist * (u.n//2 - 1)
                min_row_l += u_size * 5 + u_dist * (5 - 1)
            diff_vect_all = vector_clipping(diff_vect_all, min_row_l, max_row_l)
            
            
            # Find the minimal distance assignment of the unit to the proportions
            # only necessary if we are issueing the command
            if send_command:
    
                units = list(selected_units).copy()
                
                # Find middle points in the proportions
                divs = vect_middles_of_linspace(start_pos,end_pos,len(units))
                
                units_pos = np.array([np.array(u.pos.int_tuple) for u in units])
    
                pd = pairwise_distances(units_pos, divs)
                row_ind, col_ind = linear_sum_assignment(pd) 
                print(pd, row_ind, col_ind)
                
                # Reordering in order to minimize the assignment cost
                for ri,ci in zip(row_ind,col_ind): selected_units[ci] = units[ri]
                
                
            # Calculate formations position
            start_ends = vect_linspace(start_pos, start_pos - diff_vect_all, len(self.selected_units))
            for (cur_start, cur_end),u in zip(start_ends, selected_units):
                
                # diff_vect clipping
                diff_vect = cur_start-cur_end
                max_row_l = u_size * u.n//2 + u_dist * (u.n//2 - 1)
                min_row_l = u_size * 5 + u_dist * (5 - 1)
                diff_vect = vector_clipping(diff_vect, min_row_l, max_row_l)

                # Get the formation                 
                formation = get_formation_from_front_line(cur_start, diff_vect, 
                                                          u, self.screen, send_command)
                if send_command:  
                    assert len(formation) == u.n, str(len(formation))
                    u.move_at_pos(None, formation)     
                    
                pygame.draw.circle(self.screen, GREEN, cur_start.int_tuple , 2)
                pygame.draw.circle(self.screen, GREEN, cur_end.int_tuple, 2)    
        
    
    
    
    
    
    
    
    
    
    
    
    
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

    
    
    

    