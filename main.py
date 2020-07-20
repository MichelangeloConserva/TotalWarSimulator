import random
import numpy as np
import imageio

import pygame
import pymunk
import pymunk.pygame_util

from pymunk.pygame_util import to_pygame, from_pygame
from pymunk.vec2d import Vec2d
from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_q, MOUSEBUTTONDOWN, MOUSEBUTTONUP
from time import time
from io import BytesIO
from PIL import Image

from armies import Army
from utils.pymunk_utils import create_space
from utils.geom_utils import do_polygons_intersect

WIDTH, HEIGHT = 800, 600
UNIT_SIZE = 10
RED = (255, 0, 0)
GREEN = (0, 255, 0)
GHOST_RED = (255, 0, 0, 100)
DARKGREEN = pygame.color.THECOLORS["darkgreen"]
LEFT = 1
RIGHT = 3


class Game:
    
    def __init__(self, u_att = (2,0,0), u_def = (1,0,0), record = True):

        self.objects = []
        self.video = []
        self.fps = 60.0 
        self.done = False
        self.record = record
    
        pygame.init()
        
        self.font = pygame.font.Font(pygame.font.get_default_font(), 8)
        
        self.space = create_space(WIDTH, HEIGHT)
        self.screen = pygame.display.set_mode((WIDTH,HEIGHT)) 
        self.clock = pygame.time.Clock()
        self.initiate_armies(u_att, u_def)
    
    
    def update(self, dt):
        self.space.step(dt)
        self.attacker.update(dt)
        self.defender.update(dt)
    
    
    def initiate_armies(self, u_att, u_def):
        self.attacker = Army(self, (WIDTH/2, 5*HEIGHT/6), WIDTH, HEIGHT, 
                              units = u_att)
        self.defender = Army(self, (WIDTH/2, HEIGHT/6), WIDTH, HEIGHT, 
                              units = u_def, role = "Defender")
        self.attacker.enemy, self.defender.enemy = self.defender, self.attacker
        self.armies = [self.attacker, self.defender]
        
        
    def on_mouse_down(self, event):
        
        if event.button == LEFT and not self.drag_lmb:
            self.start_pos_lmb = event.pos
            self.drag_lmb = True
        
        elif event.button == RIGHT:
            self.start_pos_rmb = event.pos
            
            # for u in self.defender.units:
            #     if u.body.contains_vect(Vec2d(self.start_pos_rmb)):
            #         for au in self.selected_units: 
            #             au.set_dest(u.body.position, (u.body.position - au.body.position).angle)
            #             return
            self.drag_rmb = True    
                
                
    def on_mouse_up(self, event):
        
        if event.button == LEFT:
            if self.drag_lmb:
                self.end_pos = event.pos
                
                # Selecting units that are inside the rectangle selection
                vertices = [self.start_pos_lmb, 
                            (self.start_pos_lmb[0], self.end_pos[1]),
                            self.end_pos,
                            (self.end_pos[0], self.start_pos_lmb[1])] 
                
                selection = False
                for u in self.attacker.units:
                    if do_polygons_intersect(vertices, 
                                             [to_pygame(s.body.position, self.screen) 
                                              for s in u.soldiers]):
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
            start_pos = Vec2d(from_pygame(self.start_pos_rmb, self.screen))
            end_pos = Vec2d(from_pygame(event.pos, self.screen))
            
            if len(self.selected_units) != 0:
                self.attacker.move_units_with_formation(self.selected_units, 
                                                        start_pos, end_pos,
                                                        send_command=True)
            self.drag_rmb = False    
    
    
    def draw(self):
        if self.drag_rmb:
            start_pos = Vec2d(from_pygame(self.start_pos_rmb, self.screen))
            end_pos = Vec2d(from_pygame(pygame.mouse.get_pos(), self.screen))
            
            # Drawing the ghost of the formation
            self.attacker.move_units_with_formation(self.selected_units, 
                                                    start_pos, end_pos,
                                                    send_command = False)
        
        if self.drag_lmb:
            # Drawing the ghost of the selection
            mp = pygame.mouse.get_pos()
            if sum((np.array(self.start_pos_lmb)-np.array(mp))**2) > 30**2:
                point_list = [self.start_pos_lmb, (self.start_pos_lmb[0], mp[1]), mp, (mp[0], self.start_pos_lmb[1])]
                pygame.draw.polygon(self.screen, RED,  point_list, 3)    
        
        # Drawing pymunk objects
        # self.space.debug_draw(self.draw_options)
        for a in self.armies: a.draw()
    
    
    def move_units_with_formation(self, start_pos, end_pos, send_command = False):

        if len(self.selected_units) != 0:
            pass

                        
    
    
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
