import pymunk
import pygame
import math
import numpy as np
import math

from pymunk.pygame_util import to_pygame
from pymunk.vec2d import Vec2d



RED = (255, 0, 0)
BLACK = (0, 0, 0)
WHITE = (255, 255, 255)
GREEN = (0, 255, 0)
GHOST_RED = (255, 0, 0, 100)
DARKGREEN = pygame.color.THECOLORS["darkgreen"]

def add_box(space, pos, w, h, rot, col, mass):

    body = pymunk.Body()
    space.add(body)

    body.position = Vec2d(pos)
    body.angle = rot
    
    shape = pymunk.Poly.create_box(body, (h, w), 0.0)
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


def draw_text(t, screen, font, pos, angle):
    text = font.render(t, True, WHITE)
    text = pygame.transform.rotate(text, angle)
    text_rect = text.get_rect(center=to_pygame(pos, screen))
    screen.blit(text, text_rect)


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
            inf.shape.collision_type = 2 if role == "Attacker" else 1
            self.infantry.append(inf)
            
            
            
        self.game = game
        
    def update(self, dt):
        for u in self.units: u.update(dt)


class InfantryUnit:

    start_w = 30
    start_h = 10
    min_w, max_w = 10, 50
    # start_w = 20
    # start_h = 10
    # min_w, max_w = 10, 30
    mass = 1
    base_speed = 90
    max_speed = 220
    
    text = "INF"
    
    @property
    def dist(self): return 5
    @property
    def shape(self): return list(self.body.shapes)[0]
    @property
    def pos(self): return self.body.position
    
    def __init__(self, game, pos, rot, col):
        self.health = 100
        self.speed = self.base_speed
        self.is_selected = False
        self.fighting_against = set()
        self.disengage = False
        
        
        self.w, self.h = InfantryUnit.start_w, InfantryUnit.start_h
        body = add_box(game.space, pos, self.w, self.h, rot, col, self.mass)
        body.unit = self
        self.set_dest(pos, body.angle)
        
        game.objects.append(self)
        
        self.body = body
        self.game = game
        self.col = col

    def set_dest(self, dest, angle):
        if len(self.fighting_against) > 0:
            self.disengage = True
        
        self.dest = dest; self.dest_angle = angle
 
    def change_shape(self, w, h):
        self.game.space.remove(self.shape)
        
        shape = pymunk.Poly.create_box(self.body, (h, w), 0.0)
        shape.mass = self.mass
        shape.friction = 0.7
        shape.color = self.col
        self.game.space.add(shape)
        
        self.w, self.h = w, h





    def move(self, dt, prop = 1):

        if type(self.dest) == np.array: dest = Vec2d(self.dest.tolist())
        elif type(self.dest) != Vec2d:  dest = Vec2d(self.dest)
        else:                           dest = self.dest
        
        speed = self.speed * prop#* dt
        
        dd = ( dest - self.body.position).get_length_sqrd()
        # print(dd)
        if dd > 3:
            
            if dd < 10: speed = 2
            
            if dd < 8:
                self.body.velocity = 0,0
                self.body.position = dest
                self.body.angle = self.dest_angle
                self.change_shape(self.w , self.h)
            else:
                
                self.body.angle = (self.dest - self.body.position).angle
                dv = Vec2d(speed, 0.0)
                self.body.velocity = self.body.rotation_vector.cpvrotate(dv) 
            
        else:
            self.body.velocity = 0,0
            self.body.angular_velocity = 0.0
            
            # TODO : put this in a better pos
            if len(self.fighting_against) == 0: self.body.angle = self.dest_angle
            # self.body.position = dest
    
    def update(self, dt):
        
        
        
        prop = 1
        
        if len(self.fighting_against) > 0:
            if not self.disengage:
                angle = (list(self.fighting_against)[0].body.position-self.body.position).angle
                self.body.unit.set_dest(self.body.position, angle)
            
            
            no_enemy_in_range = True
            for e in self.fighting_against:
                
                e_bb = e.shape.bb
                e_bb.expand(e_bb.center()+Vec2d(e.w/2*1.1,e.h/2*1.1))
                e_bb.expand(e_bb.center()+Vec2d(-e.w/2*1.1,-e.h/2*1.1))
                
                if e_bb.contains(self.shape.bb): no_enemy_in_range = False
            
            print(no_enemy_in_range)
            if not no_enemy_in_range:
                prop = 0.8
            else:
                self.disengage = False

        self.move(dt, prop)


        

    def draw(self):
        shape = self.shape
        verts = []
        for v in shape.get_vertices():
            x = v.rotated(shape.body.angle)[0] + shape.body.position[0]
            y = v.rotated(shape.body.angle)[1] + shape.body.position[1]
            verts.append(to_pygame((x, y), self.game.screen))
        pygame.draw.polygon(self.game.screen, self.col, verts)
        
        draw_text(self.text, self.game.screen, self.font, self.pos, math.degrees(self.body.angle-np.pi/2))
        
        if self.is_selected:
            pos = shape.bb.left, shape.bb.top
            w = shape.bb.right - shape.bb.left
            h = shape.bb.top - shape.bb.bottom
            p = to_pygame(pos, self.game.screen)
            pygame.draw.rect(self.game.screen, RED, (*p, w, h), 2)    
    

















