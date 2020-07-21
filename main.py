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

WIDTH, HEIGHT = 1200, 800
UNIT_SIZE = 10
RED = (255, 0, 0)
GREEN = (0, 255, 0)
GHOST_RED = (255, 0, 0, 100)
DARKGREEN = pygame.color.THECOLORS["darkgreen"]
LEFT = 1
RIGHT = 3


DEBUG = True




DIST = 2
rest_length_intra = DIST + 10
stifness_intra    = 10
dumping_intra     = 2


# def add_intra_spring(unit):
#     for j in range(3):
#         for i in range(10):
#             u = unit.formation[j][i]
            
#             # bot neighbour
#             if j + 1 < 3:
#                 n_r = unit.formation[j+1][i]
#                 spring1 = pymunk.DampedSpring(u.body, n_r.body, Vec2d(), Vec2d(), 
#                                               rest_length = rest_length_intra, 
#                                               stiffness = stifness_intra * 10, 
#                                               damping = dumping_intra / 3)
#                 space.add(spring1)
            
#             # right neighbour
#             if i + 1 < 10:
#                 n_r = unit.formation[j][i+1]
#                 spring1 = pymunk.DampedSpring(u.body, n_r.body, Vec2d(), Vec2d(), 
#                                               rest_length = rest_length_intra, 
#                                               stiffness = stifness_intra , 
#                                               damping = dumping_intra)
#                 space.add(spring1)


def spring_to_mantain(s, pos, space):
    pos_static_body = pymunk.Body(body_type = pymunk.Body.STATIC)
    pos_static_body.position = pos
    
    spring1 = pymunk.DampedSpring(s.body, pos_static_body, Vec2d(), Vec2d(), 
                                  rest_length = 0, 
                                  stiffness = 15, 
                                  damping = 1)        
    space.add(spring1)    


def begin_solve_ally(arbiter, space, _):
    # s1 starts the collision
    s1, s2 = arbiter.shapes
    
    # if  not s1.sensor and not s2.sensor:
    #     spring_to_mantain(s1, s2.body.position)
    #     spring_to_mantain(s2, s2.body.position)
    
    # if not s1.sensor and not s2.sensor: return False
    # if s1.sensor and not s2.sensor:
    #     print("entering")
    #     s1.body.soldier.col = GREEN
    # elif s2.sensor and not s1.sensor:
    #     print("entering")
    
    return True

def separate_solve_ally(arbiter, space, _):
    # s1 starts the collision
    s1, s2 = arbiter.shapes
    
    # if  not s1.sensor and not s2.sensor:
    #     spring_to_mantain(s1, s2.body.position)
    #     spring_to_mantain(s2, s2.body.position)
    
    # if s1.sensor and not s2.sensor:
    #     print("exting")
    #     s1.body.soldier.col = RED
    # elif s2.sensor and not s1.sensor:
    #     print("exting")
    
    return True

def begin_solve_enemy(arbiter, space, _):
    # s1 starts the collision
    s1, s2 = arbiter.shapes
    
    # if not s1.sensor and not s2.sensor:
    #     print("impulse")
    #     f = s1.mass * s1.body.velocity
    #     s2.body.apply_impulse_at_world_point(f, s2.body.position)
        
        
    
    return True

def separate_solve_enemy(arbiter, space, _):
    # s1 starts the collision
    s1, s2 = arbiter.shapes
    
    # if  not s1.sensor and not s2.sensor:
    #     spring_to_mantain(s1, s2.body.position, s1.body.soldier.game.space)
    #     spring_to_mantain(s2, s2.body.position, s1.body.soldier.game.space)
    
    return True




class Game:
    
    def __init__(self, u_att = (3,0,0), u_def = (1,0,0), record = True):

        self.objects = []
        self.video = []
        self.fps = 60.0 
        self.done = False
        self.attack_command = False
        self.record = record
    
        pygame.init()
        self.font = pygame.font.Font(pygame.font.get_default_font(), 15)
        
        
        
        self.space = create_space(WIDTH, HEIGHT)

        CH_22 = self.space.add_collision_handler(2, 2)
        CH_11 = self.space.add_collision_handler(1, 1)        
        CH_12 = self.space.add_collision_handler(2, 1)        
        
        CH_22.begin = begin_solve_ally
        CH_11.begin = begin_solve_ally
        CH_22.separate = separate_solve_ally
        CH_11.separate = separate_solve_ally
        
        CH_12.begin = begin_solve_enemy
        
        
        self.screen = pygame.display.set_mode((WIDTH,HEIGHT)) 
        self.clock = pygame.time.Clock()
        self.initiate_armies(u_att, u_def)
        
        
        
        if DEBUG:
            from pymunk.pygame_util import DrawOptions
            self.draw_options = DrawOptions(self.screen)


        
        
    
    
    def update(self, dt):
        self.space.step(dt)
        self.attacker.update(dt)
        self.defender.update(dt)
    
    
    def initiate_armies(self, u_att, u_def):
        self.attacker = Army(self, (WIDTH/2, 4*HEIGHT/6), WIDTH, HEIGHT, 
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
            
            if len(self.selected_units) == 0: return
            
            self.start_pos_rmb = Vec2d(from_pygame(event.pos, self.screen))
            
            for u in self.defender.units:
                for s in u.soldiers:
                    if (s.body.position - self.start_pos_rmb).length < s.radius*2+s.dist:
                        print("attack")
                        self.attack_command  = True
                        for su in self.selected_units:
                            su.target_unit = u
                        
                        return
                
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
            
            if self.attack_command:
                self.attack_command = False
                return
            
            start_pos = self.start_pos_rmb
            end_pos = Vec2d(from_pygame(event.pos, self.screen))
            
            if len(self.selected_units) != 0:
                self.attacker.move_units_with_formation(self.selected_units, 
                                                        start_pos, end_pos,
                                                        send_command=True)
            self.drag_rmb = False    
    
    
    def draw(self):
        if self.drag_rmb:
            start_pos = self.start_pos_rmb
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
                    self.save_video()   # might take some time to run
                    return
            
                elif event.type == MOUSEBUTTONDOWN: self.on_mouse_down(event)
                elif event.type == MOUSEBUTTONUP:   self.on_mouse_up(event)
        
            
            self.update(1/self.fps)
            
            if DEBUG: self.space.debug_draw(self.draw_options)
            
            self.draw()
            
            fps = self.font.render(str(int(self.clock.get_fps())), True, pygame.Color('black'))
            self.screen.blit(fps, (50, 50))            
                
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
