import pymunk
import pygame

from pymunk.pygame_util import to_pygame

from base_classes import Person

WHITE = (255, 255, 255)

class Melee_Soldier(Person):
    
    # density = 1
    friction = 1
    elasticity = 0.03
    mass = 90
    
    # radius = 5
    # dist = 2.5
    radius = 10
    dist =   5
    
    melee_range = 1
    max_speed   = 90
    base_speed  = 120
    # max_speed   = 300
    # base_speed  = 350
    
    def __init__(self, game, pos, col, coll):
        Person.__init__(self, game, pos, col, coll)
    
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
        pygame.draw.circle(self.game.screen, self.col, pos, self.radius-1)

        if self.unit.is_selected:
            pygame.draw.circle(self.game.screen, WHITE, pos, self.radius-3)





































