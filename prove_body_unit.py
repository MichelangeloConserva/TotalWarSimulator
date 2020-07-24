import numpy as np
import pymunk
import pygame

from pymunk.vec2d import Vec2d
from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_q, K_SPACE
from pymunk.pygame_util import to_pygame, DrawOptions
from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment

from testing_utils import App, fps, WIDTH, HEIGHT, BLACK, GREEN, RED
from utils.unit_utils import get_formation



space = pymunk.Space((WIDTH, HEIGHT))


class Holder:
    def __init__(self, pos, radius=5, coll=1):
        self.body = pymunk.Body()
        self.body.position = pos
        shape = pymunk.Circle(self.body, radius)
        shape.density = 0.01
        shape.friction = 0.5
        shape.elasticity = 1
        shape.collision_type = coll
        space.add(self.body, shape)

class Soldier:

    radius = 7
    dist = 4
    
    mass = 5
    friction = 1
    elasticity = 0.01

    
    def __init__(self, pos, col = BLACK, coll = 1):
        
        body = pymunk.Body()
        body.position = pos
        body.soldier = self
        self.body = body

        shape = pymunk.Circle(self.body, self.radius)
        shape.color = col
        shape.friction = self.friction
        shape.elasticity = self.elasticity
        shape.mass = self.mass
        shape.collision_type = coll      

        space.add(body, shape)
        
        self.enemy_in_range = set()
        self.col = col
        self.shape = shape
        
    # def draw(self):
    #     pos = to_pygame(self.body.position,screen)
    #     pygame.draw.circle(screen, self.col, pos, self.radius-1)


class Unit:
    
    s = Soldier
    start_n = 100
    start_ranks = 5
    dist = 5
    start_width = (start_n//start_ranks)*s.radius*2 + (start_n//start_ranks-1) * s.dist        
    rest_length_intra = s.radius*2 + s.dist
    aggressive_charge = False    
    
    @property
    def soldier_size_dist(self): return self.s.radius*2, self.s.dist
    @property
    def soldiers_pos(self): return [s.body.position for s in self.soldiers]
    @property
    def n(self): return len(self.soldiers)
    
    def __init__(self, pos, angle, col = RED, coll = 1):
        
        self.pos = pos
        self.col = col
        self.coll = coll
        self.n_ranks = self.start_ranks    
        self.angle = angle        
        
        self.body = pymunk.Body()
        self.body.position = pos
        self.body.angle = angle
        space.add(self.body)
        
        self.add_soldiers()
        
        
        size, dist = self.soldier_size_dist
        self.order, r_ind = get_formation(pos, self.angle, self.n_ranks, self.start_n, size, dist)
        self.execute_formation(self.order, r_ind, set_physically = True)        

    def add_soldiers(self):        
        soldiers = []
        for _ in range(self.start_n):
            s = Soldier(Vec2d(), self.col, self.coll)
            s.unit = self
            s.body.angle = self.angle - np.pi/2
            soldiers.append(s)        
        self.soldiers = soldiers

    def execute_formation(self, formation, ranks_ind, set_physically = False):
        
        # Storing the rank information
        self.ranks = [[] for _ in range(max(ranks_ind.values())+1)]
        
        pd = pairwise_distances(formation, self.soldiers_pos)
        row_ind, col_ind = linear_sum_assignment(pd) 
        for i in range(len(row_ind)):
            d = Vec2d(formation[i].tolist())
            
            if set_physically:  
                self.soldiers[col_ind[i]].body.position = d
                # self.soldiers[col_ind[i]].holder.position = d
            
            # Each soldiers knows its destination and is in a particular rank
            # self.soldiers[col_ind[i]].set_dest(d)
            self.ranks[ranks_ind[i]].append(self.soldiers[col_ind[i]])          


    def update(self, dt):
        
        pass
        
        # self.body.apply_force_at_local_point(Vec2d(self.s.mass * self.n * 10,
        #                                            np.random.randn()), Vec2d(0,0))
        
        # if self.attacking and all([u.collided for u in self.units]): 
        #     self.attacking = False
        
        # if self.attacking:
        #     for u in self.units:
        #         if not u.collided and u.body.velocity.length < 4:
        #             u.body.apply_force_at_local_point(Vec2d(speed,np.random.randn()), Vec2d(0,0))
    









app = App(space)


u2 = Unit((WIDTH/2, 2*HEIGHT/6), np.pi, GREEN)
u1 = Unit((WIDTH/2, 4*HEIGHT/6), 0, RED)




alls = [u1,u2]

while app.running:
    for event in pygame.event.get():
        app.do_event(event)

    app.draw()
    app.clock.tick(fps)


    for u in alls: u.update(1/60.) 
    space.step(1/fps)

pygame.quit()
