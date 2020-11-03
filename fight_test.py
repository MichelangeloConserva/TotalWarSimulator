import numpy as np
import pygame
import pymunk
import matplotlib.pyplot as plt

from pymunk.vec2d import Vec2d
from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_SPACE
from scipy import interpolate
from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment
from shapely.geometry import Point
from time import time
from collections import namedtuple
from pymunk.pygame_util import to_pygame, from_pygame

from Unit import Unit
from utils.colors import WHITE, BLUE, GREEN, RED
from utils.pymunk_utils import calc_vertices
from utils.pymunk_utils import do_polygons_intersect
from game import Game

np.set_printoptions(precision=3, suppress=1)


record = False
DEBUG = True

def game_loop(game, stop):

  for event in pygame.event.get():
    if event.type == QUIT or event.type == KEYDOWN and event.key == K_ESCAPE:
      pygame.quit()
      game.done = True
      if game.record:
        game.save_video()  # might take some time to run
    if event.type == KEYDOWN and event.key == K_SPACE:
      return not stop
  
    if event.type == pygame.MOUSEBUTTONDOWN:
      if event.button == 3:
        game.record_mouse = True
        game.mouse_traj = [game.get_mouse_pos(False)]
      if event.button == 1:
        global selected_unit
        
        mouse_pos = game.get_mouse_pos(False)
        for u in units:
          inf_simplices, _ = u.formation.get_hulls()  
          if inf_simplices.contains(Point(*mouse_pos)):
            
            if not selected_unit is None: selected_unit.is_selected = False
            u.is_selected = True
            selected_unit = u
            return
        
        if not selected_unit is None: selected_unit.is_selected = False
        selected_unit = None
        
    if event.type == pygame.MOUSEBUTTONUP:
      if event.button == 3:
        game.record_mouse = False
        
    if game.record_mouse:
        game.mouse_traj.append(game.get_mouse_pos(False))



game = Game(record)
dt = 1 / game.fps


units_1 = []
for i in [200,400]:
  units_1.append(Unit(game, (i, 600), np.pi, RED, 1))
units_2 = []
for i in [200,400]:
  units_2.append(Unit(game, (i, 300), np.pi, GREEN, 2))

units = units_1 + units_2


selected_unit = None


stop = False
while not game.done:
  game.screen.fill(WHITE)
  stop = game_loop(game, stop)
  if game.done: break

  if not stop:
    game.update(dt)
    
    for u in units:
      u.update(dt)

    if not game.record_mouse and len(game.mouse_traj) > 0:
      
      if len(game.mouse_traj) > 3:
        for s in selected_unit.soldiers: s.trajectory = []
        _, start_dir = selected_unit.pos_direction
        final_dir = None
        trajectory = np.array(game.mouse_traj)
        trajectory = trajectory + np.random.randn(len(trajectory), 2)*0.0001
        selected_unit.set_spline_formation_trajectory(start_dir, final_dir, trajectory, True)
        game.mouse_traj = []
  
        
  game.draw(DEBUG)
  for u in units:
    u.draw(DEBUG)
  
  # for cur_traj_form in traj_form:
  if not selected_unit is None:
    traj_form = selected_unit.traj
    for i in range(len(traj_form)-1):
      p1 = to_pygame(traj_form[i], game.screen)
      p2 = to_pygame(traj_form[i+1], game.screen)
      pygame.draw.aalines(game.screen, (0,0,0), False, [p1,p2])
  
  pygame.display.flip()
  game.clock.tick(game.fps)

  if game.record:
    arr = pygame.surfarray.array3d(game.screen).swapaxes(1, 0)
    game.video.append(arr.astype(np.uint8))





# %%   OLD CODE FOR TRAJECTORY SPLINE NOW IMPLEMENTED IN THE UNIT


# angle_th = 50
# SPLINE_DISTANCE_POINT = 50

# def check_dir_angle(dir_angle): return  dir_angle > angle_th

# def get_n_points(length): return max(int(length/SPLINE_DISTANCE_POINT),2)

# def get_spline(trajectory, show = False):
#   if show:
#     plt.scatter(*trajectory.T)
    
#   length = sum((np.diff(trajectory,axis=0)**2).sum(1)**0.5)
#   f, u = interpolate.splprep(trajectory.T.tolist(), s=0, k=2)
#   inter_points = np.linspace(0, 1, get_n_points(length))
#   xint, yint = interpolate.splev(inter_points, f)
#   trajectory = [Vec2d(list(t)) for t in np.vstack((xint, yint)).T]
  
#   directions = np.diff(trajectory,axis=0)
#   directions = (directions / ((directions**2).sum(1)**0.5).reshape(-1,1)) #* int(length/80)
  
#   if show:
#     for i in range(len(xint)-1):
#       plt.plot((xint[i],xint[i]+directions[i][0]), 
#                (yint[i],yint[i]+directions[i][1]), color = "red")

#   return trajectory, [Vec2d(list(d.round(2))) for d in directions]

# def get_cur_traj(trajectory, directions, i, last_dir):
#   cur_pos = Vec2d(trajectory[i])
  
#   if i < len(directions): cur_dir = directions[i]
#   else:
#     if final_dir is None: cur_dir = cur_pos - Vec2d(trajectory[i-1])
#     else:                           cur_dir = last_dir
    
#   return cur_pos, cur_dir, Vec2d(trajectory[i+1]) if i+1 < len(trajectory)  else None

# def get_formation_at(formation, unit, pos, angle):
#   pos = np.array(list(pos))
#   n_ranks, n = unit.n_ranks, unit.n
#   size, dist = unit.soldier_size_dist
#   return formation.get_formation(pos, angle, n_ranks, n, size, dist)

# def set_formation(unit, f1, ranks_ind, traj_form, is_LSA):

#   if is_LSA:
#     # print("CHANGE",i, dir_angle)
#     ranks = [[] for _ in range(max(ranks_ind.values()) + 1)]
    
#     pd = pairwise_distances(f1, traj_form[-1])
#     row_ind, col_ind = linear_sum_assignment(pd)
    
#     soldiers = []
#     for i in range(len(row_ind)):
#       d = Vec2d(f1[i].tolist())
#       s = unit.soldiers[col_ind[i]]
#       ranks[ranks_ind[i]].append(s)
#       coord = list((ranks_ind[i], len(ranks[ranks_ind[i]]) - 1))
#       s.trajectory.append((d, coord))
#       soldiers.append(s)
#     unit.soldiers = soldiers

#   for i,s in enumerate(unit.soldiers):
#     d = Vec2d(f1[i].tolist())
#     if len(s.trajectory) > 0: coord = s.trajectory[-1][1] 
#     else: coord = s.coord
#     s.trajectory.append((d, coord))
  

# def update_soldiers_trajectory(unit):
#   for s in unit.soldiers:
#     s.target_position, s.coord = s.trajectory[0]
#     s.changed_target = False
  
  
# def set_spline_formation_trajectory(formation, start_dir, final_dir, trajectory, show = False):
#   spline_trajectory, spline_directions = get_spline(np.array(trajectory), show)
  
#   last_dir = start_dir
#   traj_form = [unit.get_soldiers_pos(False)]
#   for i in range(len(spline_trajectory)):
    
#     cur_pos, cur_dir, next_pos = get_cur_traj(spline_trajectory, spline_directions, i, last_dir)
#     dir_angle = abs((last_dir).get_angle_degrees_between(cur_dir)) 
#     formation_angle = (cur_dir).perpendicular().angle  
    
#     if show: plt.scatter(*cur_pos, color = "blue" if check_dir_angle(dir_angle) < 50 else "red")
    
#     f1, ranks_ind = get_formation_at(formation, unit, np.array(list(cur_pos)), formation_angle)
#     set_formation(unit, f1, ranks_ind, traj_form, check_dir_angle(dir_angle))
    
#     last_dir = cur_dir
#     traj_form.append(f1)
    
#   update_soldiers_trajectory(unit)
    
#   return spline_trajectory