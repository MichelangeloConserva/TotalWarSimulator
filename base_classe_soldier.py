import pymunk
import pygame

from pymunk.pygame_util import to_pygame
from pymunk.vec2d import Vec2d

from utils.pymunk_utils import limit_velocity


class Person:
    
    def __init__(self, game, pos, col, coll):
        
        # Linking
        self.order = pos
        self.col = col
        self.game = game
        
        # Physics stuff
        self.body = self.add_body(pos)
        self.body.velocity_func = limit_velocity     
        self.shape = self.add_shape(coll)
        self.sensor = self.add_sensor(coll)
        game.space.add(self.body, self.shape, self.sensor)
        
        # Variables
        self.enemy_melee_range = set()
        self.enemy_in_range = set()
        self.reset_nn()
        self.target_soldier = None
    
    def reset_nn(self):
        self.front_nn = None
        self.bot_nn = None
        self.left_nn = None
        self.right_nn = None
    
    def dies(self):
        self.game.space.remove(self.body, self.shape, self.sensor, self.spring)
    
    def set_dest(self, pos): self.order = pos
    
    def draw(self):
        pos = to_pygame(self.body.position,self.game.screen)
        pygame.draw.circle(self.game.screen, self.col, pos, self.radius)
        
        # draw_text(str(len(self.enemy_in_range)), screen, font, pos, 
        #           math.degrees(np.pi/2-self.body.angle))
        
    
    def add_body(self, *args, **kwargs):   raise NotImplementedError("add_body")
    def add_shape(self, *args, **kwargs):  raise NotImplementedError("add_shape")
    def add_sensor(self, *args, **kwargs): raise NotImplementedError("add_sensor")
    def update(self, *args, **kwargs):     raise NotImplementedError("add_sensor")
