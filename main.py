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
from time import time
from io import BytesIO
from PIL import Image

from characters import Army
from utils import create_space, is_in_selection, is_on_enemy_unity
from characters import move_units_with_formation, ghost_units_with_formation

WIDTH, HEIGHT = 800, 800
UNIT_SIZE = 10
RED = (255, 0, 0)
GREEN = (0, 255, 0)
LEFT = 1
RIGHT = 3
    

def update(space, dt, surface):
    space.step(dt)
    attacker.update(dt)
    defender.update(dt)
    
    
space = create_space(WIDTH, HEIGHT)

pygame.init()
screen = pygame.display.set_mode((WIDTH,HEIGHT)) 


clock = pygame.time.Clock()
draw_options = pymunk.pygame_util.DrawOptions(screen)



attacker = Army(space, (WIDTH/2, HEIGHT/6), WIDTH, HEIGHT, units = (5,0,0))
defender = Army(space, (WIDTH/2, 5*HEIGHT/6), WIDTH, HEIGHT, units = (1,0,0), role = "Defender")

attacker.enemy = defender
defender.enemy = attacker

attacker.screen = screen
defender.screen = screen


video = []
k = 0
drag_lmb = drag_rmb = False
start_pos_lmb = 0,0
selected_units = set()
while True:
    screen.fill(pygame.color.THECOLORS["white"])
    
    for event in pygame.event.get():
        if event.type == QUIT or \
            event.type == KEYDOWN and (event.key in [K_ESCAPE, K_q]): 
            pygame.quit()
    
        elif event.type == MOUSEBUTTONDOWN:
            
            if event.button == LEFT and not drag_lmb:
                start_pos_lmb = event.pos
                drag_lmb = True
            
            elif event.button == RIGHT:
                start_pos_rmb = from_pygame(event.pos, screen)
                
                enemy_unit = is_on_enemy_unity(start_pos_rmb, attacker)
                if not enemy_unit is None:
                    for u in selected_units: u.attack(enemy_unit)
                else:
                    drag_rmb = True
                    
                
        elif event.type == MOUSEBUTTONUP:
            
            if drag_lmb:
                end_pos = from_pygame(event.pos, screen)
                
                selection = False
                for u in attacker.units:
                    for s in u.soldiers:
                        if is_in_selection(list(s.body.position), from_pygame(start_pos_lmb, screen), end_pos): 
                            u.is_selected = selection = True
                            selected_units.add(u)
                            break
                drag_lmb = False
                
                if not selection:
                    for u in attacker.units: u.is_selected = False
                    selected_units = set()                
                         
            if event.button == RIGHT:
                end_pos_rmb = from_pygame(event.pos, screen)
                
                if sum((np.array(start_pos_rmb)-np.array(end_pos_rmb))**2) < 30**2:
                    for u in selected_units: u.dest = start_pos_rmb
                else:
                    ghost_units_with_formation(selected_units, 
                                               to_pygame(start_pos_rmb, screen), 
                                               pygame.mouse.get_pos(), screen, 
                                               send_command = True)
                drag_rmb = False


    if drag_rmb:
        ghost_units_with_formation(selected_units, to_pygame(start_pos_rmb, screen), pygame.mouse.get_pos(), screen)
    
    if drag_lmb:
        mp = pygame.mouse.get_pos()
        if sum((np.array(start_pos_lmb)-np.array(mp))**2) > 30**2:
            point_list = [start_pos_lmb, (start_pos_lmb[0], mp[1]), mp, (mp[0], start_pos_lmb[1])]
            pygame.draw.polygon(screen, RED,  point_list, 3)
        
    
    space.debug_draw(draw_options)
    fps = 60.0
    update(space, 1/fps, screen)
    pygame.display.flip()
    
    clock.tick(fps)
    
    # pygame.image.save(screen, f"imgs/{k}.png")
    # k += 1
    video.append(pygame.surfarray.array3d(screen).swapaxes(1, 0).astype(np.uint8))




# Saving the result
import imageio
imageio.mimwrite('output_filename.gif', video , fps = 60.)






    