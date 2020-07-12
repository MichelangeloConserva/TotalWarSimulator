"""
https://github.com/viblo/pymunk/blob/master/examples/tank.py
"""
import random
import numpy as np

import pygame
import pymunk
import pymunk.pygame_util

from pymunk.vec2d import Vec2d
from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_q, MOUSEBUTTONDOWN, MOUSEBUTTONUP
from pymunk.pygame_util import from_pygame, to_pygame

from characters import Army
from utils import create_space, is_in_selection, is_on_enemy_unity

WIDTH, HEIGHT = 800, 800
UNIT_SIZE = 10
RED = (255, 0, 0)
LEFT = 1
RIGHT = 3
    
def update(space, dt, surface):
    space.step(dt)
    for s in attacker.soldiers: s.update(dt)
    
    
space = create_space(WIDTH, HEIGHT)

attacker = Army(space, (WIDTH/2, HEIGHT/6), WIDTH, HEIGHT)
defender = Army(space, (WIDTH/2, 5*HEIGHT/6), WIDTH, HEIGHT, units = (1,0,0), role = "Defender")

attacker.enemy = defender
defender.enemy = attacker


pygame.init()
screen = pygame.display.set_mode((WIDTH,HEIGHT)) 
clock = pygame.time.Clock()
draw_options = pymunk.pygame_util.DrawOptions(screen)


drag = False
start_pos = 0,0
selected_units = set()
while True:
    
    
    for event in pygame.event.get():
        if event.type == QUIT or \
            event.type == KEYDOWN and (event.key in [K_ESCAPE, K_q]): 
            pygame.quit()
    
    
        elif event.type == MOUSEBUTTONDOWN:
            
            if event.button == LEFT:
                start_pos = from_pygame(event.pos, screen)
                drag = True
            
            elif event.button == RIGHT:
                p = from_pygame(event.pos, screen)
                
                enemy_unit = is_on_enemy_unity(p, attacker)
                if not enemy_unit is None:
                    for u in selected_units: u.attack(enemy_unit)
                else:
                    for u in selected_units: u.move_at_pos(p)
                
        elif event.type == MOUSEBUTTONUP:
            
            if drag:
                end_pos = from_pygame(event.pos, screen)
                
                selection = False
                for u in attacker.units:
                    for s in u.soldiers:
                        if is_in_selection(list(s.body.position), start_pos, end_pos): 
                            u.is_selected = selection = True
                            selected_units.add(u)
                            break
                drag = False
                
                if not selection:
                    for u in attacker.units: u.is_selected = False
                    selected_units = set()                
                                
    
    screen.fill(pygame.color.THECOLORS["white"])
    space.debug_draw(draw_options)
    fps = 60.0
    update(space, 1/fps, screen)
    pygame.display.flip()
    
    clock.tick(fps)
    
    

























    