import numpy as np
import imageio
import pygame

from pymunk.pygame_util import to_pygame, from_pygame, DrawOptions
from pymunk.vec2d import Vec2d
from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_q, MOUSEBUTTONDOWN, MOUSEBUTTONUP

from armies import Army
from utils.pymunk_utils import create_space
from utils.geom_utils import do_polygons_intersect
from utils.collisions_utils import begin_solve_ally, begin_solve_enemy, begin_solve_utils
from utils.collisions_utils import separate_solve_enemy, separate_solve_ally
from utils.enums import Role

WIDTH, HEIGHT = 1500, 800
UNIT_SIZE = 10
RED = (255, 0, 0)
GREEN = (0, 255, 0)
GHOST_RED = (255, 0, 0, 100)
DARKGREEN = pygame.color.THECOLORS["darkgreen"]
LEFT = 1
RIGHT = 3
DEBUG = False

space_damping = 0.9

class Game:
    
    def __init__(self, u_att = (5,0,0), u_def = (3,0,0), record = True):

        self.objects = []
        self.video = []
        self.fps = 60.0 
        self.done = False
        self.attack_command = False
        self.record = record
    
        self.create_screen()
        self.create_space()
        self.initiate_armies(u_att, u_def)
        
        # Draw physics related stuff
        if DEBUG: self.draw_options = DrawOptions(self.screen)


    def create_screen(self):
        pygame.init()
        self.font = pygame.font.Font(pygame.font.get_default_font(), 8)
        self.screen = pygame.display.set_mode((WIDTH,HEIGHT)) 
        self.clock = pygame.time.Clock()
        
    def create_space(self):
        self.space = create_space(WIDTH, HEIGHT)
        self.space.damping = space_damping
        
        
        CH_33 = self.space.add_collision_handler(3, 3)   # Utils
        CH_32 = self.space.add_collision_handler(3, 2)   # Utils
        CH_31 = self.space.add_collision_handler(3, 1)   # Utils
        CH_22 = self.space.add_collision_handler(2, 2)
        CH_11 = self.space.add_collision_handler(1, 1)        
        CH_12 = self.space.add_collision_handler(2, 1)        
        
        # Collisions between allies
        CH_22.begin = begin_solve_ally
        CH_11.begin = begin_solve_ally
        CH_22.separate = separate_solve_ally
        CH_11.separate = separate_solve_ally
        
        # Collisions between enemies
        CH_12.begin = begin_solve_enemy
        CH_12.separate = separate_solve_enemy
        
        # Formation schelenton
        CH_33.begin = begin_solve_utils
        CH_32.begin = begin_solve_utils
        CH_31.begin = begin_solve_utils
    
    
    def update(self, dt):
        self.space.step(dt)
        self.attacker.update(dt)
        self.defender.update(dt)
    
    
    def initiate_armies(self, u_att, u_def):
        self.attacker = Army(self, (WIDTH/2, 4*HEIGHT/6), WIDTH, HEIGHT, 
                              units = u_att)
        self.defender = Army(self, (WIDTH/2, 2*HEIGHT/6), WIDTH, HEIGHT, 
                              units = u_def, role = Role.DEFENDER)
        self.attacker.enemy, self.defender.enemy = self.defender, self.attacker
        self.armies = [self.attacker, self.defender]
        
        ### TEST TIME ###
        # for u in self.attacker.units:
        #     pos = u.pos
        #     enemy = [(e.pos-pos).length for e in self.defender.units]
        #     u.target_unit = self.defender.units[np.argmin(enemy)]
        
        
        
    def on_mouse_down(self, event):
        
        if event.button == LEFT and not self.drag_lmb:
            # On left click unselect the units
            for u in self.attacker.units: u.is_selected = False
            self.selected_units = set()        
            
            # Start dragging
            self.start_pos_lmb = event.pos
            self.drag_lmb = True
        
        
        elif event.button == RIGHT:
            if len(self.selected_units) == 0: return  # Nothing to command
            
            # If we click on enemy units all the selected units will attack
            self.start_pos_rmb = Vec2d(from_pygame(event.pos, self.screen))
            for u in self.defender.units:
                for s in u.soldiers:
                    if (s.body.position - self.start_pos_rmb).length < sum(u.soldier_size_dist):
                        self.attack_command  = True
                        for su in self.selected_units: su.target_unit = u
                        return
            # Otherwise we are dragging
            self.drag_rmb = True    
                
    def on_mouse_up(self, event):
        
        if event.button == LEFT:
            if self.drag_lmb:
                self.end_pos = event.pos
                
                # Selecting units that are inside the rectangle selection
                vertices = [self.start_pos_lmb, 
                            (self.start_pos_lmb[0], self.end_pos[1]),
                            self.end_pos,
                            (self.end_pos[0], self.start_pos_lmb[1])] 
                for u in self.attacker.units:
                    if do_polygons_intersect(vertices, 
                                             [to_pygame(s.body.position, self.screen) 
                                              for s in u.soldiers]):
                        u.is_selected = True
                        self.selected_units.add(u)
                self.drag_lmb = False
                
                
        elif event.button == RIGHT:
            
            # If we have sent an attack command we are done
            if self.attack_command:
                self.attack_command = False
                return
            
            # The formation command is sent
            start_pos = self.start_pos_rmb
            end_pos = Vec2d(from_pygame(event.pos, self.screen))
            if len(self.selected_units) != 0:
                self.attacker.move_units_with_formation(self.selected_units, 
                                                        start_pos, end_pos,
                                                        send_command=True)
            self.drag_rmb = False    
    
    
    def draw(self, DEBUG):
        if self.drag_rmb:
            start_pos = self.start_pos_rmb
            end_pos = Vec2d(from_pygame(pygame.mouse.get_pos(), self.screen))
            
            # Drawing the ghost of the formation
            self.attacker.move_units_with_formation(self.selected_units, 
                                                    start_pos, end_pos,
                                                    send_command = False)
        
        if self.drag_lmb:
            # Drawing the ghost of the selection
            mp = pygame.mouse.get_pos()
            if sum((np.array(self.start_pos_lmb)-np.array(mp))**2) > 30**2:
                point_list = [self.start_pos_lmb, (self.start_pos_lmb[0], mp[1]), mp, (mp[0], self.start_pos_lmb[1])]
                pygame.draw.polygon(self.screen, RED,  point_list, 3)    

        # Drawing all the units
        for a in self.armies: a.draw(DEBUG)
    
    
    def run(self):
            
        self.selected_units = set()
        self.drag_lmb = self.drag_rmb = False
        
        while True:
            self.screen.fill(pygame.color.THECOLORS["white"])
            
            for event in pygame.event.get():
                
                if event.type == QUIT or \
                    event.type == KEYDOWN and (event.key in [K_ESCAPE, K_q]): 
                    pygame.quit()
                    self.done = True
                    if self.record: self.save_video()   # might take some time to run
                    return
            
                elif event.type == MOUSEBUTTONDOWN: self.on_mouse_down(event)
                elif event.type == MOUSEBUTTONUP:   self.on_mouse_up(event)
        
            
        
        
            self.update(1/self.fps)
            
            
            if DEBUG: self.space.debug_draw(self.draw_options)
            self.draw(DEBUG)
            
            
            fps = self.font.render(str(int(self.clock.get_fps())), True, pygame.Color('black'))
            self.screen.blit(fps, (50, 50))            
                
            pygame.display.flip()
            self.clock.tick(self.fps)
            
            
            if self.record:
                arr = pygame.surfarray.array3d(self.screen).swapaxes(1, 0)
                self.video.append(arr.astype(np.uint8))
        
    def save_video(self): 
        from pygifsicle import optimize
        imageio.mimwrite('test_.gif', self.video , fps = self.fps)
        optimize("test_.gif")



if __name__ == "__main__":
    game = Game(record = True)
    game.run()
    
