import pymunk
import pygame
import numpy as np

from pymunk.vec2d import Vec2d
from pymunk.pygame_util import to_pygame

from utils.unit_utils import get_formation
from utils.enums import Coll


# TODO : add base class formation


stiffness, damping = 5 * 100, 10
RED = (255, 0, 0)


class Node(pymunk.Circle):
    
    radius = 7
    
    def __init__(self, body, offset):
        pymunk.Circle.__init__(self, body, self.radius, offset)
        self.sensor = True
        self.mass = 1
        self.friction = 0.01
        self.elasticity = 1
        self.collision_type = Coll.UTILS.value
        self.reset_nns()
        self.soldier = None
    
    def reset_nns(self):
        self.front_nn = None
        self.back_nn = None
        self.left_nn = None
        self.right_nn = None
    
    def get_nns(self): return self.front_nn, self.back_nn, self.left_nn, self.right_nn

class RectFormation:
    
    col = pygame.Color("blue")
    
    def __init__(self, game, pos, angle, n_ranks, n, unit):
        self.pos = pos
        self.angle = angle
        self.n_ranks = n_ranks
        self.n = n
        self.unit = unit
        self.game = game
        
        self.body = pymunk.Body()
        self.body.position = pos
    
        self.body.formation = self
        self.create_formation(-np.pi/2)
        self.body.angle = angle
        
        
        game.space.add(self.body)
        
        
    def create_formation(self, angle):   
        
        size, dist = self.unit.s_type.radius*2, self.unit.s_type.dist
        formation, r_ind = get_formation(np.zeros(2), angle, self.n_ranks, self.n, size, dist)        
        

        self.formation = [[] for _ in range(max(r_ind.values())+1)]
        
        for i in range(len(formation)):
                
            f = Vec2d(formation[i].tolist())
            n = Node(self.body, f)
            self.game.space.add(n)
            
            self.formation[r_ind[i]].append(n)      
        
        for i in range(len(self.formation)-1):
            for j,n in enumerate(self.formation[i]):
                if i-1 >= 0:
                    n_l = self.formation[i-1][j]
                    n_l.right_nn = n
                    n.left_nn = n_l
                if j-1 >= 0:
                    n_f = self.formation[i][j-1]
                    n_f.back_nn = n
                    n.front_nn = n_f
                    
        # The last rank must be treated differently since it may have less soldiers
        l_rank = self.formation[-1]
        for j in range(len(l_rank)):
            n = self.formation[-1][j]
    
            if j + 1 < len(l_rank): 
                n_f = l_rank[j+1]
                n_f.front_nn = n
                n.right_nn = n_f
                
            # We must find the front neighbour manually
            front_nn = self.formation[-2][np.argmin([(n.offset-o.offset).length for o in self.formation[-2]])]
            n.front_nn = front_nn
            front_nn.back_nn = n




    def attach_soldiers(self, physically=False):
        
        if physically:
            k = 0
            for i in range(len(self.formation)):
                for n in self.formation[i]:
                    s = self.unit.soldiers[k]
                    pos = self.body.local_to_world(n.offset)
                    
                    s.node, n.soldier = n, s
                    s.body.position = pos
                    s.set_dest(pos)
                    
                    # joint = pymunk.DampedSpring(s.body, self.body, Vec2d(), n.offset, 0, stiffness, damping)
                    # joint.collide_bodies = False
                    # self.game.space.add(joint)
                    
                    k+=1

    def draw(self):
        for s in self.body.shapes:
            pos = to_pygame(self.body.local_to_world(s.offset),self.game.screen)
            for nn in s.get_nns():
                if nn is None:continue
                p = to_pygame(self.body.local_to_world(nn.offset),self.game.screen)
                pygame.draw.aalines(self.game.screen, RED, False, [pos,p])             
        
        
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
        
if __name__ == "__main__":

    import numpy as np
    import pymunk
    import pygame
    
    from pymunk.vec2d import Vec2d
    from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_q, K_SPACE
    from scipy.optimize import linear_sum_assignment
    
    from testing_utils import App, fps, WIDTH, HEIGHT, BLACK, GREEN, RED
    from utils.unit_utils import get_formation
    
    
    
    space = pymunk.Space((WIDTH, HEIGHT))
    app = App(space)
    
    
    from units import Melee_Unit
    u2 = RectFormation(app, Vec2d(300, 300), np.pi, 5, 100, Melee_Unit)
    
    b = pymunk.Body()
    b.position = u2.body.local_to_world(Vec2d(100,0))
    s = pymunk.Circle(b, 10)
    s.color = RED
    s.mass = 50
    space.add(s,b)
    
    # u2.body.angle += np.pi/2
    
    while app.running:
        for event in pygame.event.get():
            app.do_event(event)
        
        app.clock.tick(fps)
        space.step(1/fps)
        
        u2.body.angle += 1/fps
        b.position = u2.body.local_to_world(Vec2d(100,0))
        
        app.draw()
        
        # u2.draw()

    pygame.quit()
        