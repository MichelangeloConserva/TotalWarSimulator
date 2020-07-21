import numpy as np
import pygame

from pymunk.vec2d import Vec2d
from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment
from pymunk.pygame_util import to_pygame, from_pygame

from soldiers import Melee_Soldier
from utils.unit_utils import get_formation
from utils.pygame_utils import draw_text

# HURRY_THRESHOLD = 0.3


def calc_width(n, n_ranks):
    return (n/n_ranks)*Melee_Soldier.radius + (n/n_ranks-1) * Melee_Soldier.dist

class Melee_Unit:
    
    start_n = 50 # 100
    start_ranks = 5
    start_width = calc_width(start_n, start_ranks)
    dist = 5
    
    @property
    def soldier_size_dist(self): return Melee_Soldier.radius*2, Melee_Soldier.dist
    @property
    def max_width(self): return calc_width(self.n, 2)
    @property
    def min_width(self): return calc_width(self.n, 2)
    @property
    def width(self):     return calc_width(self.n, self.n_ranks)
    @property
    def pos(self): return sum(self.soldiers_pos) / len(self.soldiers)
    @property
    def soldiers_pos(self): return [s.body.position for s in self.soldiers]
    
    
    def __init__(self, game, pos, angle, col, coll):
        
        size, dist = Melee_Soldier.radius*2, Melee_Soldier.dist
        formation, _ = get_formation(pos, angle, self.start_ranks, 
                                     self.start_n, size, dist)
        
        soldiers = []
        for i,p in enumerate(formation[::-1]):
            s = Melee_Soldier(game, Vec2d(p.tolist()), col, coll)
            # s = Melee_Soldier(game, Vec2d(p), col, coll)
            s.unit = self
            soldiers.append(s)
        
        self.formation = formation
        self.soldiers = soldiers
        self.col = col
        self.game = game
        
        self.n = self.start_n
        self.n_ranks = self.start_ranks
        
        self.is_selected = False
        
        nps = np.array(soldiers)
        self.ranks = nps.reshape((self.n_ranks, -1))#.tolist()
        
        
        self.order = None
        self.before_order = None
        
        
    def min_cost_assignment(self, formation):
        pd = pairwise_distances(self.soldiers_pos, formation)
        row_ind, col_ind = linear_sum_assignment(pd) 
        for i in range(len(row_ind)):
            d = Vec2d(formation[col_ind[i]].tolist())
            self.soldiers[row_ind[i]].set_dest(d)        
        
        
    def move_at(self, formation, formation_first, ranks_ind):
        
        self.before_order = self.soldiers_pos.copy()
        
        self.order = formation
        self.order_time = pygame.time.get_ticks() 
        
        
        formation = formation_first
        # Front Line is occupied first
        s_left = self.soldiers.copy()
        for r in ranks_ind:
            c_form = formation[r]
            soldiers_pos = [s.body.position for s in s_left]
            pd = pairwise_distances(soldiers_pos, c_form)
            row_ind, col_ind = linear_sum_assignment(pd) 
                
            for i in range(len(row_ind)):
                s_left[row_ind[i]].set_dest(Vec2d(c_form[col_ind[i]].tolist()))      
            s_left = [s_left[i]  for i in range(len(s_left))  if i not in row_ind]
                


    def update(self, dt):
    
        ds = [(s.body.position-s.order).length for s in self.soldiers]
    
        if not self.order is None:
            # if pygame.time.get_ticks()  - self.order_time > 2000 or max(ds) < Melee_Soldier.radius/2:
            if max(ds) < Melee_Soldier.radius/2:
                self.min_cost_assignment(self.order)
                self.order = None
                
                # Create springs
                for s in self.soldiers:
                    for s1 in self.soldiers:
                        if s == s1: continue
                    
                        d = (s.body.position-s1.body.position).length
                        if d - (s.dist + s.radius*2) < 1:
                            print(s.dist, d)
                        
                
                
                

    
        # Moving the soldiers
        
        # hurry_th = np.quantile(ds, HURRY_THRESHOLD)
        # max_d = max(ds)
        
        for i,s in enumerate(self.soldiers):
            
            d = ds[i]
            
            if d < Melee_Soldier.radius/2: 
                s.body.velocity = 0,0
            
            # if d < hurry_th: speed = s.base_speed #+ (s.max_speed-s.base_speed) * (d / max_d)
            # else:            speed = s.base_speed
            speed = s.base_speed
        
            
            direction = s.order - s.body.position 
            s.body.angle = direction.angle



            if not self.order is None:

                if (s.order - s.body.position).get_length_sqrd() < 1:
                    s.body.velocity = 0,0
                else:
                    dv = Vec2d(speed, 0.0)
                    s.body.velocity = s.body.rotation_vector.cpvrotate(dv)           
        
            else:
                # Using IMPULSE
                # Killing lateral velocity
                lat_dir = s.body.local_to_world(Vec2d(1,0)) - s.body.position
                lat_vel = lat_dir.dot(s.body.velocity) * lat_dir
                imp = s.body.mass * -lat_vel
                s.body.apply_force_at_world_point(imp, s.body.position)
                
                
                s.body.angular_velocity = 0.
                s.body.angle = direction.angle
                s.body.apply_force_at_local_point(Vec2d(speed * s.mass,0), Vec2d(0,0))
                # s.body.apply_impulse_at_local_point(Vec2d(speed,0), Vec2d(0,0))
        
        
        for s in self.soldiers: s.update(dt)
        
                
    
    def draw(self):
        for s in self.soldiers: s.draw()

        # for i,r in enumerate(self.cols):
        #     for s in r:
        #         p = to_pygame(s.body.position, self.game.screen)
        #         draw_text(str(i), self.game.screen, self.game.font, p, 0)
    
        # if not self.before_order is None:
        #     for i,s in enumerate(self.soldiers):
        #         draw_text(str(i), self.game.screen, self.game.font, self.before_order[i], 0)
        #         draw_text(str(i), self.game.screen, self.game.font, s.body.position, 0)
                
                
            
            
    
    
    
    

































        
        