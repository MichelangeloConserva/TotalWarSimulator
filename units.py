import numpy as np

from pymunk.vec2d import Vec2d
from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment

from utils.unit_utils import get_formation
from soldiers import Melee_Soldier


HURRY_THRESHOLD = 0 # 0.3


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
    def min_width(self): return calc_width(self.n, 5)
    @property
    def width(self):     return calc_width(self.n, self.cur_ranks)
    @property
    def pos(self): return sum(self.soldiers_pos) / len(self.soldiers)
    @property
    def soldiers_pos(self): return [s.body.position for s in self.soldiers]
    
    
    def __init__(self, game, pos, angle, col, coll):
        
        size, dist = Melee_Soldier.radius*2, Melee_Soldier.dist
        formation = get_formation(pos, angle, self.start_ranks, self.start_n, size, dist)
        
        soldiers = []
        for p in formation:
            s = Melee_Soldier(game, Vec2d(p), col, coll)
            s.unit = self
            soldiers.append(s)
        
        self.formation = formation
        self.soldiers = soldiers
        self.col = col
        
        self.n = self.start_n
        self.cur_ranks = self.start_ranks
        
        self.is_selected = False
        
        
    def move_at(self, formation):
        pd = pairwise_distances(self.soldiers_pos, formation)
        row_ind, col_ind = linear_sum_assignment(pd) 
        for i in range(len(row_ind)):
            self.soldiers[row_ind[i]].set_dest(formation[col_ind[i]])
        

    def update(self, dt):
    
        # Moving the soldiers
        ds = [(s.body.position-s.order).length for s in self.soldiers]
        hurry_th = np.quantile(ds, HURRY_THRESHOLD)
        max_d = max(ds)
        
        for i,s in enumerate(self.soldiers):
            d = ds[i]
            
            if d < Melee_Soldier.radius: 
                s.body.velocity = 0,0
            
            if d < hurry_th: speed = s.base_speed #+ (s.max_speed-s.base_speed) * (d / max_d)
            else:            speed = s.base_speed
        
        
            s.body.angle = (s.order - s.body.position).angle

            if (s.order - s.body.position).get_length_sqrd() < 1:
                s.body.velocity = 0,0
            else:
                dv = Vec2d(speed, 0.0)
                s.body.velocity = s.body.rotation_vector.cpvrotate(dv)           
        
            # Using IMPULSE
            # s.body.angle = (s.order - s.body.position).angle
            # s.body.apply_force_at_local_point(Vec2d(speed,0), Vec2d(0,0))
        
        
        for s in self.soldiers: s.update(dt)
        
                
    
    
    def draw(self):
        for s in self.soldiers: s.draw()











































        
        