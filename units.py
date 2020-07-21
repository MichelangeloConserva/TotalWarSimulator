import numpy as np
import pygame
import pymunk

from pymunk.vec2d import Vec2d
from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment
from pymunk.pygame_util import to_pygame, from_pygame



from soldiers import Melee_Soldier
from utils.unit_utils import get_formation
from utils.pygame_utils import draw_text


RED = (255, 0, 0)


# HURRY_THRESHOLD = 0.3

stifness_intra    = 4
dumping_intra     = 2

def calc_width(upr):
    return upr*Melee_Soldier.radius*2 + (upr-1) * Melee_Soldier.dist

class Melee_Unit:
    
    start_n = 50 # 100
    start_ranks = 5
    start_width = calc_width(start_n//start_ranks)
    dist = 5
    
    @property
    def soldier_size_dist(self): return Melee_Soldier.radius*2, Melee_Soldier.dist
    @property
    def max_width(self): return calc_width(self.n//3)
    @property
    def min_width(self): return calc_width(self.n//10)
    @property
    def width(self):     return calc_width(len(self.ranks[0]))
    @property
    def pos(self): return sum(self.soldiers_pos) / len(self.soldiers)
    @property
    def soldiers_pos(self): return [s.body.position for s in self.soldiers]
    @property
    def border_units(self): return [s for s in self.soldiers if len(s.body.constraints) <= 3]
    @property
    def target_unit(self): return self._target_unit
    @target_unit.setter
    def target_unit(self, tu):
        self._target_unit = tu
        if tu is None: return
        
        # Facing the enemy unit
        self.move_at_point(tu.pos, remove_tu=False)
        

    
    def __init__(self, game, pos, angle, col, coll):
        
        self.col = col
        self.coll = coll
        self.game = game        
        
        
        self.add_soldiers()

        
        size, dist = self.soldier_size_dist
        formation, ranks_ind = get_formation(pos, angle, self.start_ranks, 
                                     self.start_n, size, dist)
        self.execute_formation(formation, ranks_ind, set_physically = True)

        
        self.n = self.start_n
        self.n_ranks = self.start_ranks
        
        self._target_unit = None
        self.is_selected = False
        self.order = None
        self.before_order = None
        self.springs = []

        
    def add_soldiers(self):        
        soldiers = []
        for _ in range(self.start_n):
            s = Melee_Soldier(self.game, Vec2d(), self.col, self.coll)
            s.unit = self
            soldiers.append(s)        
        self.soldiers = soldiers
        
        
    def execute_formation(self, formation, ranks_ind, set_physically = False):
        
        # Storing the rank information
        self.ranks = [[] for _ in range(max(ranks_ind.values())+1)]
        
        pd = pairwise_distances(formation, self.soldiers_pos)
        row_ind, col_ind = linear_sum_assignment(pd) 
        for i in range(len(row_ind)):
            d = Vec2d(formation[i].tolist())
            
            if set_physically:  self.soldiers[col_ind[i]].body.position = d
            
            # Each soldiers knows its destination and is in a particular rank
            self.soldiers[col_ind[i]].set_dest(d)
            self.ranks[ranks_ind[i]].append(self.soldiers[col_ind[i]])        
        
        
    def move_at(self, formation, formation_first, ranks_ind, remove_tu = True):
        # self.before_order = self.soldiers_pos.copy()
        
        # Removing springs while changing formation
        for s in self.springs: self.game.space.remove(s)
        self.springs = []
        
        # Remove target unit if changing pos
        if remove_tu: self.target_unit = None
        
        # Changing formation
        self.execute_formation(formation_first, ranks_ind)

        # Order to complete after changing formation
        self.order = formation
        
        
    def move_at_point(self, final_pos, final_angle = None, n_ranks = None, remove_tu = True):

        start_pos = self.pos
        angle = (start_pos-final_pos).perpendicular().angle  # TODO : save variable for diff
        
        if final_angle is None: final_angle = angle
        if n_ranks     is None: n_ranks     = self.n_ranks
        
        
        
        # Checking if the unit formatio must be rotated
        looking_dir = Vec2d(1,0).rotated(final_angle)
        invert = looking_dir.dot((start_pos-final_pos).perpendicular()) < 0
        if invert: angle = (final_pos-start_pos).perpendicular().angle
        else:      angle = (start_pos-final_pos).perpendicular().angle


        
        size, dist = self.soldier_size_dist
        changed_formation, _ = get_formation(np.array(list(start_pos)), angle, n_ranks, 
                                     self.n, size, dist)

        final_formation, ranks_ind = get_formation(np.array(list(final_pos)), final_angle, n_ranks, 
                                     self.n, size, dist)

        self.move_at(final_formation, changed_formation, ranks_ind, remove_tu = remove_tu)
        
        
        

    # TODO : refactor
    def update(self, dt):
    
        ds = [(s.body.position-s.order).length for s in self.soldiers]
    
        if not self.order is None:
            # if pygame.time.get_ticks()  - self.order_time > 2000 or max(ds) < Melee_Soldier.radius/2:
            if max(ds) < Melee_Soldier.radius/2:
                
                
                ranks = self.ranks
                space = self.game.space
                k = 0
                for i in range(len(ranks)-1):
                    for j in range(len(ranks[i])):
                        s = ranks[i][j]
                        
                        
                        s.set_dest(Vec2d(self.order[k].tolist()))
                        k += 1

                        
                        rest_length_intra = s.radius*2 + s.dist
                        
                        # right neighbour
                        if j + 1 < len(ranks[i]):
                            n_r = ranks[i][j+1]
                            spring1 = pymunk.DampedSpring(s.body, n_r.body, Vec2d(), Vec2d(), 
                                                          rest_length = rest_length_intra, 
                                                          stiffness = stifness_intra * 10, 
                                                          damping = dumping_intra / 3)
                            space.add(spring1)
                            self.springs.append(spring1)
                            
                            # Linking neighbours
                            s.right_nn = n_r
                            n_r.left_nn = s
                            
                            
                            
                        
                        if i == len(ranks)-2: continue
                        # bot neighbour
                        if i + 1 < len(ranks):
                            n_r = ranks[i+1][j]
                            spring1 = pymunk.DampedSpring(s.body, n_r.body, Vec2d(), Vec2d(), 
                                                          rest_length = rest_length_intra, 
                                                          stiffness = stifness_intra , 
                                                          damping = dumping_intra)
                            space.add(spring1)
                            self.springs.append(spring1)
                            
                            # Linking neighbours
                            s.bot_nn = n_r
                            n_r.front_nn = s                            
                            
                            
                
                # The last rank must be treated differently since it may have less soldiers
                
                for j in range(len(ranks[-1])):
                    s = ranks[-1][j]
                
                    s.set_dest(Vec2d(self.order[k].tolist()))
                    k += 1                    
            
                    # right neighbour
                    if j + 1 < len(ranks[-1]):
                        n_r = ranks[-1][j+1]
                        spring1 = pymunk.DampedSpring(s.body, n_r.body, Vec2d(), Vec2d(), 
                                                      rest_length = rest_length_intra, 
                                                      stiffness = stifness_intra * 10, 
                                                      damping = dumping_intra / 3)
                        space.add(spring1)
                        self.springs.append(spring1)                
            
                        # Linking neighbours
                        s.right_nn = n_r
                        n_r.left_nn = s
                                    
            
            
                    front_nn = np.argmin([(s.body.position-o.body.position).length for o in ranks[-2]])
                    
                    n_r = ranks[-2][front_nn]
                    spring1 = pymunk.DampedSpring(s.body, n_r.body, Vec2d(), Vec2d(), 
                                                  rest_length = rest_length_intra, 
                                                  stiffness = stifness_intra * 10, 
                                                  damping = dumping_intra / 3)
                    space.add(spring1)
                    self.springs.append(spring1)                     
        
                    s.front_nn = n_r
                    n_r.bot_nn = s                
        
        
        
                self.order = None
            
    
    
    
    
    
    
    
    
        # Attack code
        if self.order is None and not self.target_unit is None:
    
            enemy_border_units = self.target_unit.border_units
    
    
            enemy_pos = np.array([list(s.body.position) for s in enemy_border_units])
            frontline_pos = np.array([list(s.body.position) for s in self.ranks[0]])
            
            pd = pairwise_distances(frontline_pos, enemy_pos)
            row_ind, col_ind = linear_sum_assignment(pd)
            
            for ri,ci in zip(row_ind, col_ind):
                s_our = self.ranks[0][ri]
                s_enemy = enemy_border_units[ci]
        
                s_our.set_dest(s_enemy.body.position)
            
            
            for i in range(1, len(self.ranks)):
                for s in self.ranks[i]:
                    s.set_dest(s.front_nn.body.position)
    

    
    
    
    
        # Moving the soldiers
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
                f = speed * s.mass * 10
                s.body.apply_force_at_local_point(Vec2d(f,0), Vec2d(0,0))
                # s.body.apply_impulse_at_local_point(Vec2d(speed,0), Vec2d(0,0))
                # print(s.body.force)
                
                
        
        for s in self.soldiers: s.update(dt)
        
                
    
    def draw(self):
        for s in self.soldiers: s.draw()

        for i,r in enumerate(self.ranks):
            for s in r:
                draw_text(str(i), self.game.screen, self.game.font, s.body.position, 0)
    
        # if not self.before_order is None:
        #     for i,s in enumerate(self.soldiers):
        #         draw_text(str(i), self.game.screen, self.game.font, self.before_order[i], 0)
        #         draw_text(str(i), self.game.screen, self.game.font, s.body.position, 0)
                
                
                
        # for s in self.ranks[-1]:
        #     draw_text(str(1), self.game.screen, self.game.font, s.body.position, 0)            
        #     pos = s.front_nn.body.position
        #     draw_text("f", self.game.screen, self.game.font, pos, 0)            
    
    
    
        # for s in self.border_units:
        #     draw_text(str(1), self.game.screen, self.game.font, s.body.position, 0)            

    
        if self.army.role == "Defender": return        

        
        if not self.target_unit is None:
    
            enemy_pos = np.array([list(s.body.position) for s in self.target_unit.border_units])
            frontline_pos = np.array([list(s.body.position) for s in self.ranks[0]])
            
            pd = pairwise_distances(frontline_pos, enemy_pos)
            row_ind, col_ind = linear_sum_assignment(pd)
            
            for ri,ci in zip(row_ind, col_ind):
                p1 = to_pygame(frontline_pos[ri], self.game.screen)
                p2 = to_pygame(enemy_pos[ci], self.game.screen)
        
                pygame.draw.aalines(self.game.screen, RED, False, [p1,p2])        
        
    
    
























        
        