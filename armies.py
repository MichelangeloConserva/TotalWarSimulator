import pymunk
import pygame
import math
import numpy as np
import math

from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment
from pymunk.vec2d import Vec2d
from pymunk.pygame_util import to_pygame, from_pygame

from units import Melee_Unit
from utils.geom_utils import spaced_vector, vector_clipping, vect_linspace, vect_middles_of_linspace
from utils.unit_utils import get_formation

RED = (255, 0, 0)
BLACK = (0, 0, 0)
WHITE = (255, 255, 255)
GREEN = (0, 255, 0)
BLUE = (0, 0, 255)
GHOST_RED = (255, 0, 0, 100)
DARKGREEN = pygame.color.THECOLORS["darkgreen"]



def draw_text(t, screen, font, pos, angle):
    text = font.render(t, True, WHITE)
    text = pygame.transform.rotate(text, angle)
    text_rect = text.get_rect(center = pos)
    screen.blit(text, text_rect)


# def get_formation_from_front_line(v1, front_line, u):

#     formation = []
#     perp = -front_line.perpendicular_normal()
#     u_size, u_dist = u.soldier_size_dist
#     upr = np.minimum(int((front_line).length / (u_size + u_dist)),u.n//2)
#     alphas = np.linspace(0,1, upr).tolist()[:-1]
    
#     if len(alphas) > 0:
#         n_rows_with_additional = u.n - len(alphas)*(u.n // len(alphas))
        
#     for a in alphas:
#         for r in range(u.n // len(alphas)):
#             pos = Vec2d((v1*(a) + (v1 - front_line)*(1-a) +\
#                          perp * (r * (u_size + u_dist)) ).int_tuple)
#             formation.append(pos)
            
#         if n_rows_with_additional > 0:
#             pos = Vec2d((v1*(a) + (v1 - front_line)*(1-a) +\
#                          perp * ((r+1) * (u_size + u_dist)) ).int_tuple)
#             n_rows_with_additional -= 1
#             formation.append(pos)

#     return formation
    
    

class Army:
    
    @property
    def units(self): return self.infantry   # Add cav and arch later
    
    def __init__(self, game, pos, WIDTH, HEIGHT, units = (2, 0, 0),
                 role = "Attacker"):
        angle  = np.pi if role == "Attacker" else 0
        col    = DARKGREEN if role == "Attacker" else BLUE
        coll   = 1 if role == "Attacker" else 2
        
        # Infantry
        # TODO : select a meaningful value for start ranks (10)
        formation, _  = get_formation(np.array(pos), angle, 1, units[0], Melee_Unit.start_width*units[0], 5)

        infantry = []
        for p in formation:
            inf = Melee_Unit(game, p, angle, col, coll)
            inf.army = self
            infantry.append(inf)
        
        self.infantry = infantry
        self.formation = formation
        self.game = game
        self.role = role
    
    
    def move_units_with_formation(self, selected_units, start_pos, end_pos, send_command = False):
        
        if len(selected_units) == 0: return
        
        dd = (start_pos-end_pos).get_length_sqrd()
        
        if  dd < 30**2: # Just one click
            total_w = 0
            units_pos = Vec2d((0,0))
            for u in selected_units:
                total_w += u.width + u.dist
                units_pos += u.pos / len(selected_units)
        
            front_line = (end_pos - units_pos).perpendicular_normal()
            
            start_pos = end_pos + front_line * total_w / 2 
            end_pos = end_pos - front_line * total_w / 2 
            
            start_pos = Vec2d(start_pos)
            end_pos = Vec2d(end_pos)                
        
        else:           
            start_pos = Vec2d(start_pos)
            end_pos = Vec2d(end_pos)
        
        
        diff_vect_all = start_pos-end_pos
        
        # diff_vect_all clipping
        max_row_l = min_row_l = 0
        for u in selected_units:
            max_row_l += u.max_width  + u.dist
            min_row_l += u.min_width  + u.dist
            
        diff_vect_all = vector_clipping(diff_vect_all, min_row_l, max_row_l)
        start_ends = vect_linspace(start_pos, start_pos - diff_vect_all, len(selected_units))

        units = list(selected_units)
        divs = [(start_ends[i][0]+start_ends[i][1])/2 for i in range(len(start_ends))]
        
        units_pos = np.array([list(u.pos) for u in units])

        pd = pairwise_distances(units_pos, divs)
        row_ind, col_ind = linear_sum_assignment(pd) 
        
        for ri,ci in zip(row_ind,col_ind): 
            u = units[ri]
            
            front_line = start_ends[ci][0] - start_ends[ci][1]
            width = front_line.length - u.dist*2
            
            if width > 5:
                size, dist = u.soldier_size_dist
                
                upr = width / (size+dist)
                n_ranks = int(np.ceil(u.n / upr))
                angle = front_line.angle
                
                formation, ranks_ind = get_formation(np.array(list(divs[ci])), angle, 
                                          n_ranks, u.n, size, dist)      
                
                u.angle = angle
                
                
                # if send_command:
                aa = Vec2d(1,0)
                aa.rotate(angle)
                
                invert = aa.dot((u.pos-divs[ci]).perpendicular()) < 0
                
                
                if invert: angle = (divs[ci]-u.pos).perpendicular().angle
                else:      angle = (u.pos-divs[ci]).perpendicular().angle
                
                form_first, ranks_ind_first = get_formation(np.array(list(u.pos)), angle, 
                                          n_ranks, u.n, size, dist)
                
                
                if send_command:
                    u.n_ranks = n_ranks
                    u.move_at(formation, form_first, ranks_ind)
                else:
                    for p in formation:
                        self.draw_circle(p, 10//2, RED)
    

    def draw_circle(self, pos, r, c):    
        pygame.draw.circle(self.game.screen, c, to_pygame(pos, self.game.screen), r)
    
    def draw_polygon(self, vs, c, w=1):    
        pygame.draw.circle(self.game.screen, c, [to_pygame(v, self.game.screen) for v in vs], w)
    
    
    
    def update(self, dt):
        for u in self.units: u.update(dt)

    def draw(self): 
        for u in self.units: u.draw()

        try:
            self.prova
        except:
            return            
        pos = Vec2d(self.prova)*10 + Vec2d(400,300)
        pygame.draw.circle(self.game.screen, GREEN, pos.int_tuple, 5)
        pygame.draw.circle(self.game.screen, GREEN, Vec2d(400,300), 5)
        print(self.prova)
            
            


















