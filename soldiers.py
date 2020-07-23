import pymunk
import pygame

from pymunk.pygame_util import to_pygame
from pymunk.vec2d import Vec2d

from base_classes import Person

WHITE = (255, 255, 255)
BLACK = (0, 0, 0)

stiffness, damping = 5 * 7, 1

class Melee_Soldier(Person):
    
    # density = 1
    friction = 1
    elasticity = 0.01
    mass = 5
    
    radius = 10
    dist = 5
    # radius = 2
    # dist =   1
    
    melee_range = 1
    max_speed   = 90
    base_speed  = 50
    # max_speed   = 300
    # base_speed  = 350
    
    def __init__(self, game, pos, col, coll):
        Person.__init__(self, game, pos, col, coll)
        
        self.size = 2*self.radius+self.dist
    
        body = pymunk.Body()
        body.position = pos
        shape = pymunk.Circle(body, 1)
        shape.density = 0.01
        shape.friction = 0.5
        shape.elasticity = 1
        shape.collision_type = coll
    
        joint = pymunk.constraint.DampedSpring(
            body, self.body, Vec2d(), Vec2d(), 0,  stiffness, damping)
        joint.collide_bodies = False    
        
        self.game.space.add(body, shape, joint)    
    
        self.holder = body
    
    
    def add_body(self, pos):   
        body = pymunk.Body()
        body.position = pos
        body.soldier = self
        return body
        
    def add_shape(self, coll):  
        shape = pymunk.Circle(self.body, self.radius)
        shape.color = self.col
        # shape.density = self.density
        shape.friction = self.friction
        shape.elasticity = self.elasticity
        shape.mass = self.mass
        shape.collision_type = coll      
        return shape

    def add_sensor(self, coll): 
        sensor = pymunk.Circle(self.body, self.radius + self.melee_range)
        sensor.sensor = True
        sensor.collision_type = coll    
        return sensor

    def update(self, dt,):
        pass

    def draw(self):
        pos = to_pygame(self.body.position, self.game.screen)
        pygame.draw.circle(self.game.screen, self.col, pos, self.radius)

        if self.unit.is_selected:
            pygame.draw.circle(self.game.screen, BLACK, pos, self.radius-1)
