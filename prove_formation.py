import numpy as np
import matplotlib.pyplot as plt
import pymunk
import pygame
import math
import numpy as np
import math

from pymunk.vec2d import Vec2d
from scipy.spatial.transform import Rotation as R
from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_q, MOUSEBUTTONDOWN, MOUSEBUTTONUP, K_SPACE
from pymunk.pygame_util import to_pygame, DrawOptions
from pymunk.vec2d import Vec2d

import pygame


from characters import Body

BLACK = (0, 0, 0)
RED = (255, 0, 0)
GREEN = (0, 255, 0)

debug = False

WIDTH, HEIGHT = 500, 300

speed = 200
max_speed = 500


DIST = 2
rest_length_intra = DIST + 10
stifness_intra    = 10
dumping_intra     = 1

def add_intra_spring(unit):
    for j in range(3):
        for i in range(10):
            u = unit.formation[j][i]
            
            # bot neighbour
            if j + 1 < 3:
                n_r = unit.formation[j+1][i]
                spring1 = pymunk.DampedSpring(u.body, n_r.body, Vec2d(), Vec2d(), 
                                              rest_length = rest_length_intra, 
                                              stiffness = stifness_intra * 3, 
                                              damping = dumping_intra)
                space.add(spring1)
            
            # right neighbour
            if i + 1 < 10:
                n_r = unit.formation[j][i+1]
                spring1 = pymunk.DampedSpring(u.body, n_r.body, Vec2d(), Vec2d(), 
                                              rest_length = rest_length_intra, 
                                              stiffness = stifness_intra , 
                                              damping = dumping_intra)
                space.add(spring1)


def spring_to_mantain(s,s1):
    # for the attacker
    pos_static_body = pymunk.Body(body_type = pymunk.Body.STATIC)
    pos_static_body.position = s1.body.position
    
    spring1 = pymunk.DampedSpring(s.body, pos_static_body, Vec2d(), Vec2d(), 
                                  rest_length = 0, 
                                  stiffness = 15, 
                                  damping = 1)        
    space.add(spring1)    
    



pygame.init()
screen = pygame.display.set_mode((WIDTH,HEIGHT)) 
clock = pygame.time.Clock()
font = pygame.font.Font(None, 30)

space = pymunk.Space((WIDTH,HEIGHT))
draw_options = DrawOptions(screen)


def limit_velocity(body, gravity, damping, dt):
     pymunk.Body.update_velocity(body, gravity, damping, dt)
     l = body.velocity.length
     if l > max_speed:
         scale = max_speed / l
         body.velocity = body.velocity * scale



class Soldier:
    def __init__(self, pos, radius, col = BLACK, coll = 1):
        body = pymunk.Body()
        body.position = pos
        body.soldier = self
        body.velocity_func = limit_velocity
        shape = pymunk.Circle(body, radius)
        shape.color = col
        shape.density = 5
        shape.friction = 1
        shape.elasticity = 0.01
        shape.mass = 50
        shape.collision_type = coll
        space.add(body, shape)
        
        self.collided = False
        
        self.body = body
        self.pos = pos
        self.radius = radius
        self.col = col
        
    def draw(self):
        pygame.draw.circle(screen, self.col, to_pygame(self.body.position, screen), self.radius)




def begin_solve(arbiter, sapce, _):
    # s1 starts the collision
    
    s1, s2 = arbiter.shapes
    s1.body.soldier.col = GREEN
    
    spring_to_mantain(s1, s2)
    spring_to_mantain(s2, s1)
    
    return True


def post_solve(arbiter, sapce, _):
    # s1 starts the collision
    
    s1, s2 = arbiter.shapes
    s1.body.soldier.col = GREEN
    
    s1.body.soldier.collided = True
    s2.body.soldier.collided = True
    
    return True

# separate
CH_21 = space.add_collision_handler(2, 1)
CH_22 = space.add_collision_handler(2, 2)

CH_21.begin = begin_solve
# CH_22.begin = begin_solve

CH_21.post_solve = post_solve


space.add_collision_handler(3, 3).begin = lambda **kargs : False




class Unit:
    
    @property
    def position(self): return sum([s.body.position for s in self.units]) / len(self.units)
    
    def __init__(self, dw = 0, h = HEIGHT/3, col = RED, t_col = 1, angle = np.pi/2):
        
        formation = []
        
        units = []
        for j in range(3):
            for i in range(10):
                dest = ((WIDTH/2+dw)+i*(10+DIST), h+j*(10+DIST))
                c = Soldier(dest, 5, col, t_col)
                c.body.angle = angle
                c.unit = self
                c.dest = dest
                units.append(c)
            formation += [units[-10:]]
        
        self.h = 2*5 + 5/2 + (3-1)/2*(DIST)
        
        self.angle = angle
        self.formation = formation
        self.units = units

    def update(self, dt):
        
        if self.attacking and all([u.collided for u in self.units]): 
            self.attacking = False
            # for u in self.units:
            #     u.body.velocity = Vec2d(0,0)
        
        if self.attacking:
            for u in self.units:
                if not u.collided and u.body.velocity.length < 4:
                    u.body.apply_force_at_local_point(Vec2d(speed,np.random.randn()), Vec2d(0,0))



alls = []

unit  = Unit()
unit.attacking = False

add_intra_spring(unit)

alls.append(unit)



for s in unit.units:
    if len(s.body.constraints) == 2:
        s.col = GREEN









pause = False
while True:
    screen.fill(pygame.color.THECOLORS["white"])
    fps = font.render(str(int(clock.get_fps())), True, pygame.Color('black'))
    screen.blit(fps, (50, 50))
    
    for event in pygame.event.get():
        
        if event.type == MOUSEBUTTONDOWN:
            print(pygame.mouse.get_pos())
            
            
            
            
            
            
            
        if event.type == QUIT or \
            event.type == KEYDOWN and (event.key in [K_ESCAPE, K_q]): 
            pygame.quit()    
    
        
        if event.type == KEYDOWN:
            if event.key == K_SPACE: pause = not pause

    for u in alls:
        u.update(1/60.) 
    
           
    
    if debug: space.debug_draw(draw_options)
    else:
        for u in alls:
            for s in u.units: s.draw()
    
    
    pygame.draw.circle(screen, BLACK, to_pygame(unit.position, screen), 5)
    
    head = to_pygame(unit.position+ Vec2d(unit.h,0.).rotated(unit.angle), screen)
    pygame.draw.circle(screen, BLACK, head, 5)



    
    
    pygame.display.flip()
    
    if not pause:
        space.step(1/30.)
        clock.tick(30)
    


if False:
    pygame.quit()

































