import pymunk
import pygame
import math
import numpy as np
import math

from pymunk.vec2d import Vec2d
from scipy.spatial.transform import Rotation as R


from utils import do_polygons_intersect, is_rect_circle_collision


RED = (255, 0, 0)
BLACK = (0, 0, 0)
WHITE = (255, 255, 255)
GREEN = (0, 255, 0)
GHOST_RED = (255, 0, 0, 100)
DARKGREEN = pygame.color.THECOLORS["darkgreen"]

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


def draw_text(t, screen, font, pos, angle):
    text = font.render(t, True, WHITE)
    text = pygame.transform.rotate(text, angle)
    text_rect = text.get_rect(center = pos)
    screen.blit(text, text_rect)


def calc_vertices(pos, h, w, angle):
    vs = []  
    vs.append(list(Vec2d( h/2, w/2))+[0])                
    vs.append(list(Vec2d( h/2,-w/2))+[0])                
    vs.append(list(Vec2d(-h/2,-w/2))+[0])                
    vs.append(list(Vec2d(-h/2, w/2))+[0])      

    M = np.array(vs)
    MR = R.from_euler('z', angle).apply(M)[:,:-1].tolist()

    return [pos + Vec2d(v) for v in MR]



class Army:
    
    @property
    def units(self): return self.infantry   # Add cav and arch later
    
    def __init__(self, game, center, WIDTH, HEIGHT, units = (2, 0, 0),
                 role = "Attacker"):
        # Infantry
        self.infantry = []
        ww = get_center_points(center[0], units[0], InfantryUnit.start_w, 5)
        for w in ww:
            inf = InfantryUnit(game, (w,center[1]), 
                               np.pi/2 if role == "Attacker" else -np.pi/2,
                               DARKGREEN if role == "Attacker" else (0, 0, 255))
            inf.army = self
            self.infantry.append(inf)
            
        self.game = game
        
    def update(self, dt):
        for u in self.units: u.update(dt)



def do_polygons_intersect(a, b):

    polygons = [a, b];
    minA, maxA, projected, i, i1, j, minB, maxB = None, None, None, None, None, None, None, None

    for i in range(len(polygons)):

        # for each polygon, look at each edge of the polygon, and determine if it separates
        # the two shapes
        polygon = polygons[i];
        for i1 in range(len(polygon)):

            # grab 2 vertices to create an edge
            i2 = (i1 + 1) % len(polygon);
            p1 = polygon[i1];
            p2 = polygon[i2];

            # find the line perpendicular to this edge
            normal = { 'x': p2[1] - p1[1], 'y': p1[0] - p2[0] };

            minA, maxA = None, None
            # for each vertex in the first shape, project it onto the line perpendicular to the edge
            # and keep track of the min and max of these values
            for j in range(len(a)):
                projected = normal['x'] * a[j][0] + normal['y'] * a[j][1];
                if (minA is None) or (projected < minA): 
                    minA = projected

                if (maxA is None) or (projected > maxA):
                    maxA = projected

            # for each vertex in the second shape, project it onto the line perpendicular to the edge
            # and keep track of the min and max of these values
            minB, maxB = None, None
            for j in range(len(b)): 
                projected = normal['x'] * b[j][0] + normal['y'] * b[j][1]
                if (minB is None) or (projected < minB):
                    minB = projected

                if (maxB is None) or (projected > maxB):
                    maxB = projected

            # if there is no overlap between the projects, the edge we are looking at separates the two
            # polygons, and we know there is no overlap
            if (maxA < minB) or (maxB < minA):
                return False;
    return True

class Body:
    
    @property
    def vertices(self): return calc_vertices(self.pos, self.h, self.w, self.angle)
    @property
    def area(self): return self.w * self.h
    
    def __init__(self, pos, w, h, angle, col, mass, unit):
        self.pos = Vec2d(pos)
        self.w = w
        self.h = h
        self.angle = angle
        self.col = col
        self.mass = mass
        self.unit = unit
    
    def contains_vect(self, m):
        
        a,b,_,c = self.vertices
        
        AM_dot_AB = (m-a).dot(b-a)
        AB_dot_AB = (b-a).dot(b-a)
        
        AM_dot_AC = (m-a).dot(c-a)
        AC_dot_AC = (c-a).dot(c-a)
        
        return 0 < AM_dot_AB and AM_dot_AB < AB_dot_AB and\
               0 < AM_dot_AC and AM_dot_AC < AC_dot_AC        
        
    def expanded_body(self, dt_x, dt_y = None): 
        
        if dt_y is None : dt_y = dt_x
        assert self.w + dt_x > 0
        assert self.h + dt_y > 0
        
        return calc_vertices(self.pos, self.h+dt_y, self.w+dt_x, self.angle)



class InfantryUnit:

    start_w = 50
    start_h = 20
    min_w, max_w = 10, 50
    mass = 1
    base_speed = 90
    max_speed = 220
    
    aurea_radius = 150
    attack_range = 5
    
    text = "INF"
    
    @property
    def dist(self): return 5
    @property
    def pos(self): return self.body.pos
    
    def __init__(self, game, pos, angle, col):
        self.health = 100
        self.speed = self.base_speed
        self.is_selected = False
        self.fighting_against = set()
        self.moving = False
        
        self.w, self.h = InfantryUnit.start_w, InfantryUnit.start_h
        body = Body(pos, self.w, self.h, angle, col, self.mass, self)

        self.set_dest(pos, angle)
        
        game.objects.append(self)
        
        self.body = body
        self.game = game
        self.col = col

    def set_dest(self, dest, angle):
        if len(self.fighting_against) > 0:
            self.disengage = True
        
        self.dest = dest; self.dest_angle = angle
 
    def change_shape(self, w, h):
        # TODO : changing shape gradually 
        pass


    def move(self, dt, prop = 1):

        if type(self.dest) == np.array: dest = Vec2d(self.dest.tolist())
        elif type(self.dest) != Vec2d:  dest = Vec2d(self.dest)
        else:                           dest = self.dest
        
        speed = self.speed * prop * dt
        
        dd = ( dest - self.body.pos).get_length_sqrd()
        # print(dd)
        if dd > 3:
            
            if dd < 10: speed = 2
            
            if dd < 8:
                self.body.pos = dest
                self.body.angle = self.dest_angle
                self.change_shape(self.w , self.h)
            else:
                self.body.angle = (self.body.pos-self.dest).angle
                
                dv = Vec2d(speed, 0.0)
                rotation_vector = Vec2d(-1.,0.).rotated(self.body.angle)
                self.body.pos += rotation_vector.cpvrotate(dv) 
            
            
        else:
            self.body.velocity = 0,0
            self.body.angle = self.dest_angle
    
    def update(self, dt):
        
        prop = 1
        
        
        # Checking enemy and allies nearby
        
        allies = set()
        enemies = set()
        
        for o in self.army.units:
            if o == self: continue
            if is_rect_circle_collision(self.body, o.body.pos, o.aurea_radius):
                allies.add(o)
                
        for o in self.army.enemy.units:
            if o == self: continue
        
            # TODO : remember that archers have circular attack range
            if do_polygons_intersect(self.body.expanded_body(self.attack_range), 
                                     o.body.vertices):
                enemies.add(o)
                prop = 0.15
                
                
                
        
        print("allies", allies)
        print("enemies", enemies)
        
        
        
        # if len(self.fighting_against) > 0:
        #     if not self.disengage:
        #         angle = (list(self.fighting_against)[0].body.pos-self.body.pos).angle
        #         self.body.unit.set_dest(self.body.pos, angle)
            

        self.move(dt, prop)


        

    def draw(self):
        vertices = self.body.vertices
        
        pygame.draw.polygon(self.game.screen, self.col, vertices, 3)

        draw_text(self.text, self.game.screen, self.font, self.pos, 
                  math.degrees(np.pi/2-self.body.angle))
        
        if self.is_selected:
            pygame.draw.polygon(self.game.screen, RED, vertices, 2)
 
    

















