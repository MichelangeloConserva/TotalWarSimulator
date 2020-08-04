import pygame
import math
import numpy as np

from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment
from pymunk.vec2d import Vec2d
from pymunk.pygame_util import to_pygame

from units import Melee_Unit
from utils.geom_utils import vector_clipping, vect_linspace
from utils.unit_utils import get_formation
from utils.pygame_utils import draw_text
from utils.enums import Role, UnitType

RED = (255, 0, 0)
BLACK = (0, 0, 0)
WHITE = (255, 255, 255)
GREEN = (0, 255, 0)
BLUE = (0, 0, 255)
GHOST_RED = (255, 0, 0, 100)
DARKGREEN = pygame.color.THECOLORS["darkgreen"]
CYAN = pygame.color.THECOLORS["cyan"]


class Army:
    
    @property
    def units(self): return self.infantry   # Add cav and arch later
    
    def __init__(self, game, pos, WIDTH, HEIGHT, units = (2, 0, 0),
                 role = Role.ATTACKER):
        angle  = np.pi if role == Role.ATTACKER else 0
        col    = DARKGREEN if role == Role.ATTACKER else BLUE
        coll   = 1 if role == Role.ATTACKER else 2
        
        # Infantry
        # TODO : select a meaningful value for start ranks (1)
        formation, _  = get_formation(np.array(pos), angle, 1, units[0], 
                                      Melee_Unit.start_width, 15)

        infantry = []
        for p in formation:
            inf = Melee_Unit(game, p, angle, col, coll)
            inf.army = self
            infantry.append(inf)
        
        self.infantry = infantry
        self.formation = formation
        self.game = game
        self.role = role
    
    def units_formation_at(self, p, selected_units):
        """
        Given a position it returns the margin vectors of the position with
        the rotation angle defined by the direction from the current position to
        the target position.

        Parameters
        ----------
        p : Vec2d
            The target position of the formation.
        selected_units : set
            The set of units that are to be moved.

        Returns
        -------
        start_pos : Vec2d
            The left margin of the new formation.
        end_pos : Vec2d
            The right margin of the new formation.

        """
        
        # TODO : Add archers and cavalry into the formation
        total_w = 0
        units_pos = Vec2d((0,0))
        for i,u in enumerate(selected_units):
            
            # Summing the width of the units
            total_w += u.width + (u.dist if i != len(selected_units)-1 else 0)
            total_w += sum(u.soldier_size_dist) # I am not sure why but add this is necessary
            
            # Calculating the average position of the unit before moving
            units_pos += u.pos / len(selected_units)
    
        front_line = (p - units_pos).perpendicular_normal()
        start_pos = p + front_line * total_w / 2
        end_pos = p - front_line * total_w / 2         
        
        return start_pos, end_pos
    
    def v_clipping(self, v, selected_units):
        max_row_l = min_row_l = 0
        for u in selected_units:
            max_row_l += u.max_width  + u.dist
            min_row_l += u.min_width  + u.dist
        return vector_clipping(v, min_row_l, max_row_l)

    
    # TODO : this only works for infantry
    def move_units_with_formation(self, selected_units, start_pos, end_pos, send_command = False):
        
        if len(selected_units) == 0: return
        
        # Obtaining the left and right margin defined by the mouse
        diff_vect_all = (start_pos-end_pos)
        if  diff_vect_all.get_length() < 50: # Just one click so no resize
            start_pos, end_pos = self.units_formation_at(start_pos, selected_units)
            diff_vect_all = (start_pos-end_pos)

        diff_vect_all = self.v_clipping(diff_vect_all, selected_units)
        
        
        
        # =============================================================================
        # Infantry
        # =============================================================================
        
        
        # Dividing the frontline for the units
        start_ends = vect_linspace(start_pos, start_pos - diff_vect_all, len(selected_units))

        units = list(selected_units)
        divs = [(start_ends[i][0]+start_ends[i][1])/2 for i in range(len(start_ends))]
        
        units_pos = np.array([list(u.pos) for u in units])

        pd = pairwise_distances(units_pos, divs)
        row_ind, col_ind = linear_sum_assignment(pd) 
        
        for ri,ci in zip(row_ind,col_ind): 
            u = units[ri]
            front_line = start_ends[ci][0] - start_ends[ci][1]
            width = front_line.length 
            if width > 5:
                size, dist = u.soldier_size_dist
                upr = width / (size+dist)
                n_ranks = int(np.ceil(u.n / upr))
                angle = front_line.angle
                
                if send_command:
                    # Sending the command to the unit
                    u.move_at_point(divs[ci], final_angle = angle, n_ranks = n_ranks)
                    u.n_ranks = n_ranks
                else:
                    # Just drawing the formation
                    formation, ranks_ind = get_formation(np.array(list(divs[ci])), angle, 
                                                              n_ranks, u.n, size, dist)                          
                    for p in formation:
                        self.draw_circle(p, 10//2, RED)    
                        

    def update(self, dt): 
        for u in self.units: u.update(dt)
        
        
    # %% Drawing utilities
    
    def draw_circle(self, pos, r, c):
        pygame.draw.circle(self.game.screen, c, to_pygame(pos, self.game.screen), r)
    
    def draw_polygon(self, vs, c, w=1):    
        pygame.draw.circle(self.game.screen, c, [to_pygame(v, self.game.screen) for v in vs], w)

    def draw(self, DEBUG): 
        for u in self.units: u.draw(DEBUG)

