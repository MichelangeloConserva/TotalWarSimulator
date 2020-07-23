import numpy as np
import pymunk
import pygame
import math
from PIL import Image

from pymunk.vec2d import Vec2d
from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_q, K_SPACE
from pymunk.pygame_util import to_pygame, DrawOptions

BLACK = (0, 0, 0)
RED = (255, 0, 0)
GREEN = (0, 255, 0)
WIDTH, HEIGHT = 700, 700



speed = 100 * 10000
# max_speed = 150
DIST = 2
rest_length_intra = DIST + 10
stifness_intra    = 5
dumping_intra     = 1




space = pymunk.Space()
b0 = space.static_body

fps = 30
steps = 10

BLACK = (0, 0, 0)
GRAY = (220, 220, 220)
WHITE = (255, 255, 255)


def begin(arbiter, sapce, _): 
    print("coo")
    return False
CH_33 = space.add_collision_handler(3, 3)   # Utils
CH_32 = space.add_collision_handler(3, 2)   # Utils
CH_31 = space.add_collision_handler(3, 1)   # Utils
CH_33.begin = begin
CH_32.begin = begin
CH_31.begin = begin



class App:
    def __init__(self):
        pygame.init()
        self.clock = pygame.time.Clock()
        self.screen = pygame.display.set_mode((WIDTH, HEIGHT))
        self.draw_options = DrawOptions(self.screen)
        self.running = True
        self.gif = 0
        self.images = []

    def do_event(self, event):
        if event.type == QUIT:
            self.running = False

        if event.type == KEYDOWN:
            if event.key in (K_q, K_ESCAPE):
                self.running = False

    def draw(self):
        self.screen.fill(GRAY)
        space.debug_draw(self.draw_options)
        pygame.display.update()

        text = f'fpg: {self.clock.get_fps():.1f}'
        pygame.display.set_caption(text)


class Box:
    def __init__(self, p0=(0, 0), p1=(WIDTH, HEIGHT), d=4):
        x0, y0 = p0
        x1, y1 = p1
        pts = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
        for i in range(4):
            segment = pymunk.Segment(
                space.static_body, pts[i], pts[(i+1) % 4], d)
            segment.elasticity = 1
            segment.friction = 0.5
            space.add(segment)


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
    def __init__(self, pos, radius, coll = 1, col = BLACK):
        
        self.collided = False
        
        # Body
        body = pymunk.Body()
        body.position = pos
        body.soldier = self
        
        # Concrete shape
        shape = pymunk.Circle(body, radius)
        # shape.color = col
        shape.density = 5
        shape.friction = 1
        shape.elasticity = 0.03
        # shape.mass = 50
        shape.collision_type = coll
        
        # Holder
        self.holder = Holder(pos, 2, 3)
        DampedSpring(body, self.holder.body, 0, 1000, 100)
        
        # Sensor
        sensor = pymunk.Circle(body, radius * 1)
        sensor.sensor = True
        sensor.collision_type = coll
        
        space.add(body, shape, sensor)
        # space.add(body, shape)
        
        self.enemy_in_range = set()
        
        self.body = body
        self.pos = pos
        self.radius = radius
        self.col = col
        
    # def draw(self):
    #     pos = to_pygame(self.body.position,screen)
    #     pygame.draw.circle(screen, self.col, pos, self.radius-1)

class PinJoint:
    def __init__(self, b, b2, a=(0, 0), a2=(0, 0)):
        joint = pymunk.constraint.PinJoint(b, b2, a, a2)
        joint.collide_bodies = False
        joint.distance = 300
        space.add(joint)

class DampedSpring:
    def __init__(self, b, b2, angle, stiffness, damping):
        joint = pymunk.constraint.DampedSpring(
            b, b2, Vec2d(), Vec2d(), 0,  stiffness, damping)
        joint.collide_bodies = False
        space.add(joint)

class Unit:
    def __init__(self, dw = 0, h = HEIGHT/3, col = RED, t_col = 1, rot = np.pi/2):
        
        formation = []
        
        units = []
        for j in range(3):
            for i in range(10):
                dest = ((WIDTH/2+dw)+i*(10+DIST), h+j*(10+DIST))
                c = Soldier(dest, 5, t_col, col)
                c.body.angle = rot
                c.unit = self
                c.dest = dest
                units.append(c)
            formation += [units[-10:]]
        
        
        self.formation = formation
        self.units = units

    def update(self, dt):
        
        if self.attacking and all([u.collided for u in self.units]): 
            self.attacking = False
        
        if self.attacking:
            for u in self.units:
                if not u.collided and u.body.velocity.length < 4:
                    u.body.apply_force_at_local_point(Vec2d(speed,np.random.randn()), Vec2d(0,0))


def add_intra_spring(unit):
    for j in range(3):
        for i in range(10):
            u = unit.formation[j][i]
            
            holder = u.holder.body

            # bot neighbour
            if j - 1 >= 0:
                n_r = unit.formation[j-1][i]
                joint = pymunk.PinJoint(holder, n_r.holder.body)
                joint.distance = rest_length_intra  
                joint.collide_bodies = False
                space.add(joint)
            
            # right neighbour
            if i - 1 >= 0:
                n_r = unit.formation[j][i-1]
                joint = pymunk.PinJoint(holder, n_r.holder.body)
                joint.distance = rest_length_intra    
                joint.collide_bodies = False
                space.add(joint)



if __name__ == '__main__':
    Box()

    # p = Vec2d(450, 450)
    # v = Vec2d(80, 0)
    
    # c1 = Soldier(p+2*v, 20, 2)
    # c = c1.holder
    
    # c3 = Soldier(p-2*v, 20, 2)
    # c2 = c3.holder
    
    # v = v.perpendicular()
    
    # c5 = Soldier(p-2*v, 20, 2)
    # c4 = c5.holder
    
    # c7 = Soldier(p+2*v, 20, 2)
    # c6 = c7.holder
    
    
    # PinJoint(c.body, c2.body)
    # PinJoint(c2.body, c4.body)
    # PinJoint(c4.body, c6.body)
    # PinJoint(c6.body, c.body)
    
    # PinJoint(c.body, c4.body)
    # PinJoint(c2.body, c6.body)
    
    
    alls = []

    unit  = Unit()
    unit.attacking = False
    unit_  = Unit(dw = -128)
    unit_.attacking = False
    
    unit1 = Unit(h = 2*HEIGHT/3, col = BLACK, t_col = 2, rot = -np.pi/2)
    unit1.attacking = True
    unit1_ = Unit(dw = -128, h = 2*HEIGHT/3, col = BLACK, t_col = 2, rot = -np.pi/2)
    unit1_.attacking = True
    
    add_intra_spring(unit)
    add_intra_spring(unit1)
    add_intra_spring(unit_)
    add_intra_spring(unit1_)
    
    alls.append(unit)
    alls.append(unit_)
    alls.append(unit1)
    alls.append(unit1_)

    
    
    
    # c.body.apply_impulse_at_local_point((100, 0))
    
    app = App()
    
    while app.running:
        for event in pygame.event.get():
            app.do_event(event)

        app.draw()
        app.clock.tick(fps)


        for i in range(steps):
            for u in alls: u.update(1/60.) 
            space.step(1/fps/steps)

    pygame.quit()

