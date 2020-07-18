#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Created on Fri Jul 17 14:00:39 2020

@author: michelangelo
"""

import numpy as np
import matplotlib.pyplot as plt
import pymunk
import pygame
import math
import numpy as np
import math

from pymunk.vec2d import Vec2d
from scipy.spatial.transform import Rotation as R
from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_q, MOUSEBUTTONDOWN, MOUSEBUTTONUP
from pymunk.pygame_util import *
from pymunk.vec2d import Vec2d

import pygame
from pygame.locals import *


from characters import Body

BLACK = (0, 0, 0)
RED = (255, 0, 0)

debug = False

WIDTH, HEIGHT = 500, 300

pygame.init()
screen = pygame.display.set_mode((WIDTH,HEIGHT)) 


space = pymunk.Space((WIDTH,HEIGHT))
draw_options = DrawOptions(screen)

class Circle:
    def __init__(self, pos, radius=20, col = BLACK, coll = 1):
        self.body = pymunk.Body()
        self.body.position = pos
        self.body.soldier = self
        shape = pymunk.Circle(self.body, radius)
        shape.color = col
        shape.density = 0.01
        shape.friction = 50
        shape.elasticity = 0.01
        shape.mass = 100
        shape.collision_type = coll
        space.add(self.body, shape)
        
        self.collided = False
        
        self.pos = pos
        self.radius = radius
        self.col = col
        
    def draw(self):
        pygame.draw.circle(screen, self.col, to_pygame(self.body.position, screen), self.radius)

def coll_handler(arbiter, sapce, _):
    s1, s2 = arbiter.shapes
    
    s1.body.soldier.collided = True
    s2.body.soldier.collided = True
    
    # s1.body.velocity = Vec2d(0,0)
    # s2.body.velocity = Vec2d(0,0)
    # s1.body.angular_velocity = 0.
    # s2.body.angular_velocity = 0.
    
    # print(s2.body.soldier)
    
    return True



space.add_collision_handler(2, 1).post_solve = coll_handler
space.add_collision_handler(1, 1).post_solve = coll_handler
space.add_collision_handler(2, 2).post_solve = coll_handler



class Unit:
    def __init__(self, h = HEIGHT/3, col = RED, t_col = 1, rot = np.pi/2):
        
        formation = []
        
        units = []
        for j in range(3):
            for i in range(10):
                dest = (WIDTH/2+i*(10+5), h+j*(10+5))
                c = Circle(dest, 5, col, t_col)
                c.body.angle = rot
                c.unit = self
                c.dest = dest
                units.append(c)
            formation += [units[-10:]]
        
        self.formation = formation
        self.units = units


unit  = Unit()
unit1 = Unit(h = 2*HEIGHT/3, col = BLACK, t_col = 2, rot = -np.pi/2)



for j in range(3):
    for i in range(10):
        u = unit.formation[j][i]
        
        # right neighbour
        if j + 1 < 3:
            n_r = unit.formation[j+1][i]
            spring1 = pymunk.DampedSpring(u.body, n_r.body, Vec2d(), Vec2d(), 
                                          rest_length = 15, 
                                          stiffness = 1, 
                                          damping = 1)
            space.add(spring1)
        
        # bot neighbour
        if i + 1 < 10:
            n_r = unit.formation[j][i+1]
            spring1 = pymunk.DampedSpring(u.body, n_r.body, Vec2d(), Vec2d(), 
                                          rest_length = 15, 
                                          stiffness = 1, 
                                          damping = 1)
            space.add(spring1)
        

for j in range(3):
    for i in range(10):
        u = unit1.formation[j][i]
        
        # right neighbour
        
        if j + 1 < 3:
            n_r = unit1.formation[j+1][i]
            spring1 = pymunk.DampedSpring(u.body, n_r.body, Vec2d(), Vec2d(), 
                                          rest_length = 15, 
                                          stiffness = 1, 
                                          damping = 1)
            space.add(spring1)
        
        # bot neighbour
        if i + 1 < 10:
            n_r = unit1.formation[j][i+1]
            spring1 = pymunk.DampedSpring(u.body, n_r.body, Vec2d(), Vec2d(), 
                                          rest_length = 15, 
                                          stiffness = 1, 
                                          damping = 1)
            space.add(spring1)
        
        

   

while True:
    screen.fill(pygame.color.THECOLORS["white"])
    
    for event in pygame.event.get():
        
        if event.type == MOUSEBUTTONDOWN:
            pass
            
        if event.type == QUIT or \
            event.type == KEYDOWN and (event.key in [K_ESCAPE, K_q]): 
            pygame.quit()    
    
    
    for u in unit1.units:
        if not u.collided and u.body.velocity.length < 4:
            u.body.apply_impulse_at_local_point(Vec2d(1,np.random.randn()), Vec2d(0,0))
            
        print(u.body.velocity)
    
        
    # for u in unit.units:
        
    #     dist = (u.dest - u.body.position)
        
    #     if dist.get_length_sqrd() > 3:
    #         u.body.angle = dist.angle
    #         u.body.apply_impulse_at_local_point(Vec2d(2,0), Vec2d(0,0))
        


    if debug: space.debug_draw(draw_options)
    else:
        for u in unit.units + unit1.units: u.draw()
    
    
    
    pygame.display.flip()
    
    space.step(1/60.)
    


if False:
    pygame.quit()

































