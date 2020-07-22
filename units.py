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
from utils.enums import Role, UnitType
from utils.geom_utils import kill_lateral_velocity

# =============================================================================
# SETTINGS
# =============================================================================
RED = (255, 0, 0)
FORCE_MULTIPLIER         = 10
dumping_intra            = 5
damping_def              = 1
stifness_intra           = FORCE_MULTIPLIER * 55
stifness_def             = FORCE_MULTIPLIER * 15
LATERAL_NOISE_MULTIPLIER = FORCE_MULTIPLIER
ATTACKING_MULTIPLIERS    = 0.5
TIME_TO_DEF = 1 # seconds


# Good values if all the soldiers push when attacking
# FORCE_MULTIPLIER         = 1
# dumping_intra            = 1
# damping_def              = 1
# stifness_intra           = FORCE_MULTIPLIER * 350
# stifness_def             = FORCE_MULTIPLIER * 550
# LATERAL_NOISE_MULTIPLIER = FORCE_MULTIPLIER
# ATTACKING_MULTIPLIERS    = 0.5





class Melee_Unit:
    
    soldier = Melee_Soldier
    role = UnitType.INFANTRY
    start_n = 100
    start_ranks = 5
    dist = 5
    start_width = (start_n//start_ranks)*soldier.radius*2 + (start_n//start_ranks-1) * soldier.dist        
    rest_length_intra = soldier.radius*2 + soldier.dist
    
    @property
    def soldier_size_dist(self): return self.soldier.radius*2, self.soldier.dist
    @property
    def max_width(self): return self.calc_width(self.n//3)
    @property
    def min_width(self): return self.calc_width(self.n//10)
    @property
    def width(self):     return self.calc_width(len(self.ranks[0]))
    @property
    def pos(self): return sum(self.soldiers_pos) / len(self.soldiers)
    @property
    def soldiers_pos(self): return [s.body.position for s in self.soldiers]
    @property
    def is_fighting(self):
        for s in self.soldiers:
            if len(s.enemy_melee_range)>0: return True
        return False
    @property
    def border_units(self): 
        th = 4 if self.defensive_stance else 3
        return [s for s in self.soldiers if len(s.body.constraints) <= th]
    @property
    def target_unit(self): return self._target_unit
    @target_unit.setter
    def target_unit(self, tu):
        self._target_unit = tu
        if tu is None: return
        self.move_at_point(tu.pos, remove_tu=False) # Facing the enemy unit
    @property
    def is_moving(self): return self._is_moving
    @is_moving.setter
    def is_moving(self, v):
        if not v: 
            self.start_mov_time = pygame.time.get_ticks()
        self._is_moving = v
        
    def __init__(self, game, pos, angle, col, coll):
        
        self.col = col
        self.coll = coll
        self.game = game        
        self.n = self.start_n
        self.n_ranks = self.start_ranks    
        
        self.is_selected = False
        self.is_moving = False
        self.defensive_stance = False
        self._target_unit = None
        self.order = None
        self.before_order = None
        self.intra_springs = []
        self.def_springs = []
        
        self.add_soldiers() 
        size, dist = self.soldier_size_dist
        self.order, r_ind = get_formation(pos, angle, self.n_ranks, self.n, size, dist)
        self.execute_formation(self.order, r_ind, set_physically = True)
        
    def calc_width(self, upr): 
        return upr*self.soldier.radius*2 + (upr-1) * self.soldier.dist        

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
        
        # Removing intra_springs while changing formation
        for s in self.intra_springs: self.game.space.remove(s)
        self.intra_springs = []
        
        # Remove target unit if changing pos
        if remove_tu: self.target_unit = None
        
        # Changing formation
        self.execute_formation(formation_first, ranks_ind)

        # Order to complete after changing formation
        self.order = formation
        
    def move_at_point(self, final_pos, final_angle = None, n_ranks = None, remove_tu = True):

        start_pos = self.pos

        # Checking is we need to start the first position a little bir further
        if self.is_fighting:
            # TODO : change these values
            start_pos = start_pos * 0.2 + final_pos * 0.8


        diff = (start_pos-final_pos)
        diff_p = diff.perpendicular()
        angle = diff.perpendicular().angle 
        
        if final_angle is None: final_angle = angle
        if n_ranks     is None: n_ranks     = self.n_ranks
        
        
        
        # Checking if the unit formation must be rotated
        looking_dir = Vec2d(1,0).rotated(final_angle)
        invert = looking_dir.dot(diff_p) < 0
        if invert: angle = (-diff_p).angle
        else:      angle = diff_p.angle


        size, dist = self.soldier_size_dist
        changed_formation, _ = get_formation(np.array(list(start_pos)), angle, n_ranks, 
                                     self.n, size, dist)
        final_formation, ranks_ind = get_formation(np.array(list(final_pos)), final_angle, n_ranks, 
                                     self.n, size, dist)
        self.move_at(final_formation, changed_formation, ranks_ind, remove_tu = remove_tu)
        
    
    def right_nn_spring(self, s, n_r):
        spring1 = pymunk.DampedSpring(s.body, n_r.body, Vec2d(), Vec2d(), 
                                      rest_length = self.rest_length_intra, 
                                      stiffness = s.body.mass * stifness_intra, 
                                      damping = dumping_intra)
        self.game.space.add(spring1)
        self.intra_springs.append(spring1)
        
        # Linking neighbours
        s.right_nn = n_r
        n_r.left_nn = s        
        
    def bot_nn_spring(self, s, n_r):
        spring1 = pymunk.DampedSpring(s.body, n_r.body, Vec2d(), Vec2d(), 
                                      rest_length = self.rest_length_intra, 
                                      stiffness = s.body.mass * stifness_intra , 
                                      damping = dumping_intra)
        self.game.space.add(spring1)
        self.intra_springs.append(spring1)
        
        # Linking neighbours
        s.bot_nn = n_r
        n_r.front_nn = s             

    def add_intra_springs_dest(self):
        ranks = self.ranks
        k = 0
        for i in range(len(ranks)-1):
            for j in range(len(ranks[i])):
                s = ranks[i][j]
                s.set_dest(Vec2d(self.order[k].tolist())); k += 1
                
                if j + 1 < len(ranks[i]): self.right_nn_spring(s, ranks[i][j+1])
                if i + 1 < len(ranks) and i != len(ranks)-2:  # (not last rank)
                    self.bot_nn_spring(s, ranks[i+1][j])
                    
        # The last rank must be treated differently since it may have less soldiers
        l_rank = ranks[-1]
        for j in range(len(l_rank)):
            s = ranks[-1][j]
            s.set_dest(Vec2d(self.order[k].tolist())); k += 1                    
    
            if j + 1 < len(l_rank): self.right_nn_spring(s, l_rank[j+1])
            # We must find the front neighbour manually
            front_nn = np.argmin([(s.body.position-o.body.position).length for o in ranks[-2]])
            self.bot_nn_spring(ranks[-2][front_nn], s) # N.B. order inverted

    def add_defensive_springs(self):
        
        b0 = self.game.space.static_body
        for s in self.soldiers:
            
            spring = pymunk.DampedSpring(s.body, b0, Vec2d(), s.order, 
                                          rest_length = 0, 
                                          stiffness = s.body.mass * stifness_def, 
                                          damping = damping_def)        
            self.game.space.add(spring)    
            self.def_springs.append((spring))

    def attack(self):
        
        if self.is_fighting:
            
            # Our frontline attack the nearest enemy border soldiers
            for s in self.ranks[0]: 
                if s.target_soldier is None: continue
                s.set_dest(s.target_soldier.body.position)
            
            # The rest of the soldiers follows
            for i in range(1, len(self.ranks)):
                for s in self.ranks[i]:
                    s.set_dest(None)
                    s.target_soldier = None
                
            return
        
        
        
        
                            
        
        # TODO : this code is not very efficient
        enemy_border_units = self.target_unit.border_units

        enemy_pos = np.array([list(s.body.position) for s in enemy_border_units])
        frontline_pos = np.array([list(s.body.position) for s in self.ranks[0]])
        
        pd = pairwise_distances(frontline_pos, enemy_pos)
        row_ind, col_ind = linear_sum_assignment(pd)
        
        
        # Our frontline attack the nearest enemy border soldiers
        for ri,ci in zip(row_ind, col_ind):
            s_our = self.ranks[0][ri]
            s_enemy = enemy_border_units[ci]
    
            s_our.set_dest(s_enemy.body.position)
            s_our.target_soldier = s_enemy
        
        # The rest of the soldiers follows
        for i in range(1, len(self.ranks)):
            for s in self.ranks[i]:
                s.set_dest(s.front_nn.body.position)
                s.target_soldier = s.front_nn


    def update(self, dt):
    
        if not self.defensive_stance:
            if not self.is_moving and\
                (pygame.time.get_ticks()-self.start_mov_time)/1000 > TIME_TO_DEF:
                self.defensive_stance = True
                # Add defensive stance springs
                self.add_defensive_springs()
                
        if self.defensive_stance and self.is_moving:
            self.defensive_stance = False
            
            # Remove defensive stance springs
            # for s,b in self.def_springs: self.game.space.remove(s,b)
            for s in self.def_springs: self.game.space.remove(s)
            self.def_springs = []    
        
        
        ds = []
    
        # TODO : add list of orders
        if not self.order is None:
            
            # When we reach the new formation then add intra_springs
            # TODO : add a better criterion for creating intra_springs
            # this is not very good when in fighting            
            ds = [(s.body.position-s.order).length for s in self.soldiers]
            if max(ds) < sum(self.soldier_size_dist):
                self.add_intra_springs_dest()
                self.order = None
            # print("no intra_springs defensive")


        # if not reordering and with a target unit then attack
        if self.order is None and not self.target_unit is None:
            self.attack()
    
    
        # Moving the soldiers
        is_moving = False
        for i,s in enumerate(self.soldiers):
            
            if len(ds) == 0:
                if s.order is None: continue
                else: d = (s.body.position-s.order).length
            else: d = ds[i]
                
            
            # Prevent tremors when reached position
            if d < Melee_Soldier.radius/2: 
                s.body.velocity = 0,0
                s.body.angular_velocity = 0.
                continue
            if not is_moving: is_moving = True
            
            
            if len(s.enemy_melee_range):
                speed = s.base_speed * ATTACKING_MULTIPLIERS
            else:
                speed = s.base_speed
            
            
            
            direction = s.order - s.body.position 
            s.body.angle = direction.angle


            # We change the velocity directly when intra_springs are not present
            # applying force without intra_springs is a mess
            if not self.order is None:
                dv = Vec2d(speed, 0.0)
                s.body.velocity = s.body.rotation_vector.cpvrotate(dv)           
                s.body.angular_velocity = 0.
            else:
                kill_lateral_velocity(s.body)
                f = speed * s.mass * FORCE_MULTIPLIER
                noise = np.random.randn() * s.mass * LATERAL_NOISE_MULTIPLIER
                s.body.apply_force_at_local_point(Vec2d(f,noise), Vec2d(0,0))
                
        if self.is_moving != is_moving: self.is_moving = is_moving
        
        # Not in use at the moment
        for s in self.soldiers: s.update(dt)
                
    
    def draw(self, DEBUG):
        for s in self.soldiers: s.draw()


        if not self.target_unit is None:
            
            for s in self.soldiers:
                
                if s.target_soldier is None: continue
                
                p1 = to_pygame(s.body.position, self.game.screen)
                p2 = to_pygame(s.target_soldier.body.position, self.game.screen)
        
                pygame.draw.aalines(self.game.screen, RED, False, [p1,p2]) 



        # for s in self.soldiers:
        #     draw_text(str(len(s.enemy_melee_range)), self.game.screen, self.game.font, s.body.position, 0)            

        # for i,r in enumerate(self.ranks):
        #     for s in r:
        #         draw_text(str(i), self.game.screen, self.game.font, s.body.position, 0)

        if not DEBUG: return


    
        # if not self.before_order is None:
        #     for i,s in enumerate(self.soldiers):
        #         draw_text(str(i), self.game.screen, self.game.font, self.before_order[i], 0)
        #         draw_text(str(i), self.game.screen, self.game.font, s.body.position, 0)
                
                
                
        # for s in self.ranks[-1]:
        #     draw_text(str(1), self.game.screen, self.game.font, s.body.position, 0)            
        #     pos = s.front_nn.body.position
        #     draw_text("f", self.game.screen, self.game.font, pos, 0)            
    

        # for s in self.border_units:
        #     draw_text(str(1), sel
        

        if self.army.role == Role.DEFENDER: return        

        if not self.target_unit is None:
            
            for s in self.soldiers:
                
                if s.target_soldier is None: continue
                
                p1 = to_pygame(s.body.position, self.game.screen)
                p2 = to_pygame(s.target_soldier.body.position, self.game.screen)
        
                pygame.draw.aalines(self.game.screen, RED, False, [p1,p2])        
    
     
        
    
    
























        
        