import numpy as np
import pymunk
import pygame

from pymunk.vec2d import Vec2d
from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_q, K_SPACE
from pymunk.pygame_util import to_pygame, DrawOptions

BLACK = (0, 0, 0)
RED = (255, 0, 0)
GREEN = (0, 255, 0)
WIDTH, HEIGHT = 500, 300
speed = 50
max_speed = 150
DIST = 2
rest_length_intra = DIST + 10
stifness_intra    = 10
dumping_intra     = 2

debug = True

def draw_text(t, screen, font, pos, angle):
    text = font.render(t, True, BLACK)
    text = pygame.transform.rotate(text, angle)
    text_rect = text.get_rect(center = pos)
    screen.blit(text, text_rect)

def add_intra_spring(unit):
    for j in range(3):
        for i in range(10):
            u = unit.formation[j][i]
            
            # bot neighbour
            if j + 1 < 3:
                n_r = unit.formation[j+1][i]
                spring1 = pymunk.DampedSpring(u.body, n_r.body, Vec2d(), Vec2d(), 
                                              rest_length = rest_length_intra, 
                                              stiffness = stifness_intra * 10, 
                                              damping = dumping_intra / 3)
                space.add(spring1)
            
            # right neighbour
            if i + 1 < 10:
                n_r = unit.formation[j][i+1]
                spring1 = pymunk.DampedSpring(u.body, n_r.body, Vec2d(), Vec2d(), 
                                              rest_length = rest_length_intra, 
                                              stiffness = stifness_intra , 
                                              damping = dumping_intra)
                space.add(spring1)


def spring_to_mantain(s,pos):
    # for the attacker
    pos_static_body = pymunk.Body(body_type = pymunk.Body.STATIC)
    pos_static_body.position = pos
    
    spring1 = pymunk.DampedSpring(s.body, pos_static_body, Vec2d(), Vec2d(), 
                                  rest_length = 0, 
                                  stiffness = 15, 
                                  damping = 1)        
    space.add(spring1)    


pygame.init()
screen = pygame.display.set_mode((WIDTH,HEIGHT)) 
clock = pygame.time.Clock()
font = pygame.font.Font(None, 30)

space = pymunk.Space((WIDTH,HEIGHT))
draw_options = DrawOptions(screen)

def limit_velocity(body, gravity, damping, dt):
     pymunk.Body.update_velocity(body, gravity, damping, dt)
     l = body.velocity.length
     if l > max_speed:
         scale = max_speed / l
         body.velocity = body.velocity * scale

class Soldier:
    def __init__(self, pos, radius, col = BLACK, coll = 1):
        
        # Body
        body = pymunk.Body()
        body.position = pos
        body.soldier = self
        body.velocity_func = limit_velocity
        
        # Concrete shape
        shape = pymunk.Circle(body, radius)
        shape.color = col
        shape.density = 5
        shape.friction = 1
        shape.elasticity = 0.03
        shape.mass = 50
        shape.collision_type = coll
        
        # Sensor
        sensor = pymunk.Circle(body, radius * 1)
        sensor.sensor = True
        sensor.collision_type = coll
        
        space.add(body, shape, sensor)
        
        self.collided = False
        self.enemy_in_range = set()
        
        self.body = body
        self.pos = pos
        self.radius = radius
        self.col = col
        
    def draw(self):
        pos = to_pygame(self.body.position,screen)
        pygame.draw.circle(screen, self.col, pos, self.radius-1)
        

def begin_solve(arbiter, sapce, _):
    # s1 starts the collision
    s1, s2 = arbiter.shapes
    
    if  not s1.sensor and not s2.sensor:
        s1.body.soldier.col = GREEN
        
        spring_to_mantain(s1, s2.body.position)
        spring_to_mantain(s2, s2.body.position)
    
    elif s1.sensor and not s2.sensor:
        s1.body.soldier.enemy_in_range.add(s2)
    elif s2.sensor and not s1.sensor:
        s2.body.soldier.enemy_in_range.add(s1)
    
    return True


def separate_solve(arbiter, sapce, _):
    # s1 starts the collision
    s1, s2 = arbiter.shapes
    
    if s1.sensor and not s2.sensor:
        s1.body.soldier.enemy_in_range.remove(s2)
    elif s2.sensor and not s1.sensor:
        s2.body.soldier.enemy_in_range.remove(s1)
    
    return True


def post_solve(arbiter, sapce, _):
    # s1 starts the collision
    
    s1, s2 = arbiter.shapes
    if s1.body.soldier.col == BLACK: s1.body.soldier.col = GREEN
    
    s1.body.soldier.collided = True
    s2.body.soldier.collided = True
    
    return True

CH_21 = space.add_collision_handler(2, 1)
CH_22 = space.add_collision_handler(2, 2)
CH_21.begin = begin_solve
CH_21.post_solve = post_solve

class Unit:
    def __init__(self, dw = 0, h = HEIGHT/3, col = RED, t_col = 1, rot = np.pi/2):
        
        formation = []
        
        units = []
        for j in range(3):
            for i in range(10):
                dest = ((WIDTH/2+dw)+i*(10+DIST), h+j*(10+DIST))
                c = Soldier(dest, 5, col, t_col)
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

# video = []
# record = True
pause = False
end = False
while True:
    screen.fill(pygame.color.THECOLORS["white"])
    fps = font.render(str(int(clock.get_fps())), True, pygame.Color('black'))
    screen.blit(fps, (50, 50))
    
    for event in pygame.event.get():
        
        if event.type == QUIT or \
            event.type == KEYDOWN and (event.key in [K_ESCAPE, K_q]): 
            pygame.quit()    
            end = True
        
        if event.type == KEYDOWN:
            if event.key == K_SPACE: pause = not pause

    if end: break

    for u in alls: u.update(1/60.) 
    
    if debug: space.debug_draw(draw_options)
    else:
        for u in alls:
            for s in u.units: s.draw()
    
    pygame.display.flip()
    
    if not pause:
        space.step(1/30.)
        clock.tick()
    
            
        # if record:
        #     arr = pygame.surfarray.array3d(screen).swapaxes(1, 0)
        #     video.append(arr.astype(np.uint8))

# if record:
#     import imageio
#     imageio.mimwrite('test_collision.gif', video , fps = 60)


