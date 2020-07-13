import pymunk  
import pygame          
import numpy as np

from pymunk.vec2d import Vec2d
from pymunk.pygame_util import from_pygame, to_pygame
from scipy.optimize import linear_sum_assignment
from sklearn.metrics import pairwise_distances
from functools import reduce
from scipy.spatial.transform import Rotation as R

RED = (255, 0, 0)
GHOST_RED = (255, 0, 0, 100)
GREEN = (0, 255, 0)

def add_box(space, size, mass, pos, rot, col):
    body = pymunk.Body()
    space.add(body)

    body.position = Vec2d(pos[0], pos[1])
    body.angle = rot
    
    shape = pymunk.Circle(body, size/2)
    shape.mass = mass
    shape.friction = 0.7
    shape.color = col
    
    space.add(shape)
    
    return body        

def get_center_points(center, n, size, dist):
    dd = n * size + (n-1)*dist - size
    return center + np.linspace(-dd/2, dd/2, n)

def get_formation(pos, width, height, size, dist):
    formation = []
    ww = get_center_points(pos[0], width, size, dist)
    hh = get_center_points(pos[1], height, size, dist)
    for i in range(width):
        for j in range(height): formation.append((ww[i], hh[j]))
    return formation
    
def get_size(width, dist, size): return width * size + (width-1) * dist     

def move_units_with_formation(selected_units, start_pos, end_pos, screen):
    start_pos = Vec2d(start_pos)
    end_pos = Vec2d(end_pos)
    
    if len(selected_units) != 0:
        
        for i,u in enumerate(selected_units):
            pr = (i+1) / len(selected_units)
            cur_start = start_pos * (pr) + end_pos * (1-pr)
            cur_end   = start_pos * (1 - pr) + end_pos * (pr)

            (cur_start-cur_end)
            
def ghost_units_with_formation(selected_units, start_pos, end_pos, screen, send_command = False):
    start_pos = Vec2d(start_pos)
    end_pos = Vec2d(end_pos)
    
    if len(selected_units) != 0:
        
        diff_vect_all = start_pos-end_pos
        
        # diff_vect_all clipping
        
        max_row_length = min_row_length = 0
        for u in selected_units:
            u_size, u_dist = u.soldier_size_dist
            max_row_length += u_size * u.n//2 + u_dist * (u.n//2 - 1)
            min_row_length += u_size * 5 + u_dist * (5 - 1)
        if diff_vect_all.length > max_row_length:
            diff_vect_all = diff_vect_all.normalized() * max_row_length
        elif diff_vect_all.length < min_row_length:
            diff_vect_all = diff_vect_all.normalized() * min_row_length
                
        
        prs = np.linspace(0,1, len(selected_units)+1).tolist()
        
        for i,u in enumerate(selected_units):
            
            if send_command: formation = []
            
            u_size, u_dist = u.soldier_size_dist
            
            pr_start = prs[i]
            pr_end = prs[i+1]
            
            cur_start = start_pos*(pr_start) + (start_pos - diff_vect_all)*(1-pr_start)
            cur_end = start_pos*(pr_end) + (start_pos - diff_vect_all)*(1-pr_end)
            diff_vect = cur_start-cur_end
            
            perp_vect = (diff_vect).perpendicular_normal()
            # diff_vect clipping
            max_row_length = u_size * u.n//2 + u_dist * (u.n//2 - 1)
            min_row_length = u_size * 5 + u_dist * (5 - 1)
            if diff_vect.length > max_row_length:
                diff_vect = diff_vect.normalized() * max_row_length
            elif diff_vect.length < min_row_length:
                diff_vect = diff_vect.normalized() * min_row_length
            
            # units per row
            upr = np.minimum(int((diff_vect).length / (u_size + u_dist)),u.n//2)

            alphas = np.linspace(0,1, upr).tolist()
            alphas = alphas[:-1]
            
            if len(alphas) > 0:
                n_rows_with_additional = u.n - len(alphas)*(u.n // len(alphas))
            
            for a in alphas:
                for r in range(u.n // len(alphas)):
                    pos = Vec2d((cur_start*(a) + (cur_start - diff_vect)*(1-a) +\
                                 perp_vect * (r * (u_size + u_dist)) ).int_tuple)
                    pygame.draw.circle(screen, GHOST_RED, pos , u_size//2)
                    
                    if send_command: formation.append(to_pygame(pos, screen))
                    
                if n_rows_with_additional > 0:
                    pos = Vec2d((cur_start*(a) + (cur_start - diff_vect)*(1-a) +\
                                 perp_vect * ((r+1) * (u_size + u_dist)) ).int_tuple)
                    pygame.draw.circle(screen, GHOST_RED, pos , u_size//2)
                    n_rows_with_additional -= 1
                
                    if send_command: formation.append(to_pygame(pos, screen))
                    
            if send_command:  
                assert len(formation) == u.n
                u.move_at_pos(None, formation)        
            pygame.draw.circle(screen, GREEN, cur_start.int_tuple , 2)
            pygame.draw.circle(screen, GREEN, cur_end.int_tuple, 2)
                            
                
            
            




class Army:
    
    def __init__(self, space, center, WIDTH, HEIGHT, units = (2, 0, 0),
                 role = "Attacker"):
        # Infantry
        self.infantry = []
        ww = get_center_points(center[0], units[0],
                               get_size(InfantryUnit.start_width, Infantry.dist, Infantry.size), 
                               4 * Infantry.dist)
        for w in ww:
            self.infantry.append(
                InfantryUnit(space, (w,center[1]), 
                             np.pi/2 if role == "Attacker" else -np.pi/2,
                     pygame.color.THECOLORS["darkgreen"] if role == "Attacker" else (0, 0, 255, 255))
            )

    @property
    def units(self): return self.infantry
    @property
    def soldiers(self): return reduce(lambda acc, ele : acc + ele, [u.soldiers for u in self.units], [])
    
    def update(self, dt):
        for u in self.units: u.update(dt)

            
class InfantryUnit:
    
    max_n = 50
    start_width = 10
    start_height = 5
    
    @property
    def n(self): return len(self.soldiers)
    @property
    def soldier_size_dist(self): return Infantry.size, Infantry.dist
    @property
    def corners(self):
        min_x = min_y = np.inf; max_y = max_x = 0
        for pos in self.soldiers_pos(False):
            if pos.x < min_x: min_x = pos.x
            if pos.x > max_x: max_x = pos.x
            if pos.y < min_y: min_y = pos.y
            if pos.y > max_y: max_y = pos.y
        return [(min_x, min_y), (max_x, max_y)]
    @property
    def cur_width(self): 
        return get_size(self.width, Infantry.dist, Infantry.size)
    def _get_is_selected(self): return self._is_selected 
    def _set_is_selected(self, a):
        if a != self._is_selected:
            for s in self.soldiers:
                list(s.body.shapes)[0].color = RED if a else self.col
        self._is_selected = a
    is_selected = property(_get_is_selected, _set_is_selected, lambda x : x)       

    
    def __init__(self, space, pos, rot, col):
        self.width, self.height = self.start_width, self.start_height
        formation = get_formation(pos, self.width, self.height, Infantry.size, Infantry.dist)
        
        self.soldiers = []
        for pos in formation:
            inf = Infantry(space, pos, rot, col)
            inf.unit = self
            self.soldiers.append(inf)
        
        self.dest = None
        self.inter_dest = None
        self._is_selected = False
        self.col = col
        self.rot = rot
        self.ghost = []
        
    
    def get_formation(self, pos):
        return get_formation(pos, self.width, self.height, Infantry.size, Infantry.dist)
    
    def move_at_pos(self, pos, new_form = None):
        if new_form is None: 
            new_form = self.get_formation(pos)    
            pos = Vec2d(pos)
            
            unit_pos = Vec2d((0,0))
            for s in self.soldiers: unit_pos += s.body.position / len(self.soldiers)
    
            M = np.array(new_form) 
            
            if (unit_pos-pos).get_length_sqrd() > 10 and  (np.pi - (unit_pos-pos).angle) > np.pi/10:
                self.rot = np.pi/2 + (unit_pos-pos).angle
            
            # Rotation of the formation
            M = M - pos
            r = R.from_euler('z', self.rot)
            MR = r.apply(np.hstack((M,np.zeros((len(M),1)))))
            MR = MR[:,:-1]
            new_form = MR + pos
        else:
            self.dest = None
        
        
        pd = pairwise_distances(self.soldiers_pos(), new_form)
        row_ind, col_ind = linear_sum_assignment(pd) # Find min cost assignment to final positions
        
        for i in range(len(row_ind)):
            try:
                self.soldiers[row_ind[i]].dest = Vec2d(new_form[col_ind[i]].tolist())
            except:
                self.soldiers[row_ind[i]].dest = Vec2d(new_form[col_ind[i]])
    
    def soldiers_pos(self, numpy = True): 
        return [np.array(a.body.position) if numpy else a.body.position for a in self.soldiers]    
    
    def attack(self, enemy_unit):
        # TODO : add attack function
        pass
    
    def update(self, dt):
        th_dist = max_dist = None
        
        if not self.dest is None:
            
            unit_pos = Vec2d((0,0))
            dds = []
            for s in self.soldiers: 
                unit_pos += s.body.position / len(self.soldiers)        
                dds.append((Vec2d(self.dest) - s.body.position).get_length_sqrd())
                
            
            if (unit_pos-Vec2d(self.dest)).get_length_sqrd() > 2 or\
                np.pi/2 + (unit_pos-Vec2d(self.dest)).angle > np.pi / 15:
                self.inter_dest = unit_pos*0.9 + Vec2d(self.dest)*0.1
                self.move_at_pos(self.inter_dest)
            else:
                
                if self.inter_dest is None:
                    self.move_at_pos(self.dest)
                else:
                    if (self.inter_dest - unit_pos).get_length_sqrd() < 10:
                        self.inter_dest = None
        
            if (self.dest - unit_pos).get_length_sqrd() < 10:
                self.dest = None        
                
            th_dist = np.mean(dds) + 2*np.std(dds); max_dist = max(dds)
        
        for s in self.soldiers: s.update(dt, th_dist, max_dist)
        
        
        if len(self.ghost) != 0:
            for pos in self.ghost: 
                pygame.draw.circl(self.army.screen, GHOST_RED, pos, Infantry.size/2)
            
        
        


class Infantry:

    size = 10
    mass = 1
    dist = size / 4
    base_speed = 90
    max_speed = 220

    def __init__(self, space, pos, rot, col):
        
        self.health = 100
        self.speed = self.base_speed

        body = add_box(space, Infantry.size, Infantry.mass, pos, rot, col)
        body.soldier = self
        
        static_body = space.static_body
        
        pivot = pymunk.PivotJoint(static_body, body, (0,0), (0,0))
        space.add(pivot)
        pivot.max_bias = 0 # disable joint correction
        pivot.max_force = 1000 # emulate linear friction
        
        gear = pymunk.GearJoint(static_body, body, 0.0, 1.0)
        space.add(gear)
        gear.max_bias = 0 # disable joint correction
        gear.max_force = 5000  # emulate angular friction 

        self.body = body
        self.dest = pos
    
    
    def update(self, dt, th_dist, max_dist):
        
        dd = (Vec2d(self.dest) - self.body.position).get_length_sqrd()
        
        if dd > 1:
            
            
            if not th_dist is None and dd > th_dist:
                self.speed = self.base_speed + (self.max_speed-self.speed) * (dd/max_dist)
            elif dd < 5:
                self.speed = 1
            else:
                self.speed = self.base_speed
            
            self.body.angle = (self.dest - self.body.position).angle
        
            if (self.dest - self.body.position).get_length_sqrd() < 1:
                self.body.velocity = 0,0

            else:
                dv = Vec2d(self.speed, 0.0)
                self.body.velocity = self.body.rotation_vector.cpvrotate(dv)            


            
            
            
            



if __name__ == '__main__':

    from utils import create_space
        
    WIDTH, HEIGHT = 640, 480
    
    space = create_space(WIDTH, HEIGHT)
    attacker = Army(space, (WIDTH/2, HEIGHT/6), WIDTH, HEIGHT)






ss = 90
max_ss = 120



ss + (max_ss - ss) * 0

























