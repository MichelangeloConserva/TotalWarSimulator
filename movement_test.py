import numpy as np
import pygame
import pymunk

from pymunk.vec2d import Vec2d
from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_SPACE
from scipy import interpolate
from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment
from time import time
from collections import namedtuple

from Unit import Unit
from utils.colors import WHITE, BLUE, GREEN, RED
from utils.pymunk_utils import calc_vertices
from utils.pymunk_utils import do_polygons_intersect
from game import Game

TrajPoint = namedtuple('TrajPoint', ['x', 'y', 'angle'])

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


angle_th = 40
# def check_dir_angle(dir_angle): return dir_angle > 50 and dir_angle < 130
def check_dir_angle(dir_angle): return dir_angle > angle_th and dir_angle < 180-angle_th
  


# def get_formation():
#   pos = np.array(list(pos))
#   n_ranks = unit.n_ranks
#   n = unit.n
#   size, dist = unit.soldier_size_dist
#   f1, ranks_ind = get_formation(pos, angle, n_ranks, n, size, dist)
#%%

game = Game(record)
dt = 1 / game.fps

k = 0
unit = Unit(game, (300, 300), np.pi, RED, 1)
soldiers = unit.soldiers
get_formation = unit.formation.get_formation
trajectory = [list(unit.pos), (675, 401), (1022, 290), (1022, 498), (798, 642), (365, 268)]

ntraj = np.array(trajectory)
length = sum((np.diff(ntraj,axis=0)**2).sum(1)**0.5)
f, u = interpolate.splprep(ntraj.T.tolist(), s=0, k=2)
xint, yint = interpolate.splev(np.linspace(0, 1, int(length/80)), f)
# xint, yint = interpolate.splev(np.linspace(0, 1, 8), f)

import matplotlib.pyplot as plt
# plt.scatter(xint, yint)
# plt.scatter(*ntraj.T)

trajectory = np.vstack((xint, yint)).T.tolist()
traj_form = []

for s in soldiers: s.trajectory = []
last_pos = unit.pos
plt.scatter(*last_pos, color = "blue")
for i,t in enumerate(trajectory[1:]):
  
  cur = Vec2d(trajectory[i+1]) 
  last = Vec2d(trajectory[i])
  two_last = Vec2d(trajectory[i-1]) if i-1 > 0  else None

  dir_angle = abs((cur-last).get_angle_degrees_between(last-two_last)) if not two_last is None else 0
  formation_angle = (cur-last).perpendicular().angle  
  
  print(dir_angle)
  plt.scatter(*cur, color = "red" if check_dir_angle(dir_angle) else "blue")

  pos = np.array(list(cur))
  n_ranks = unit.n_ranks
  n = unit.n
  size, dist = unit.soldier_size_dist
  f1, ranks_ind = get_formation(pos, formation_angle, n_ranks, n, size, dist)
  traj_form.append(f1)
  last_pos = Vec2d(t)


  if check_dir_angle(dir_angle):
    print("CHANGE",i)
    ranks = [[] for _ in range(max(ranks_ind.values()) + 1)]
    
    pd = pairwise_distances(f1, traj_form[-1])
    row_ind, col_ind = linear_sum_assignment(pd)
    
    for i in range(len(row_ind)):
      d = Vec2d(f1[i].tolist())
      s = unit.soldiers[col_ind[i]]
      ranks[ranks_ind[i]].append(s)
      coord = list((ranks_ind[i], len(ranks[ranks_ind[i]]) - 1))
      s.trajectory.append((d, coord))
    
  else:
    for i,s in enumerate(unit.soldiers):
      d = Vec2d(f1[i].tolist())
      s.target_position = d
      if len(s.trajectory) > 0: coord = s.trajectory[-1][1] 
      else: coord = s.coord
      s.trajectory.append((d, coord))



for s in soldiers:
  s.target_position, s.coord = s.trajectory[0]
  s.changed_target = False
  s.counter = 0

  

stop = False
while not game.done:
  game.screen.fill(WHITE)
  stop = game_loop(game, stop)
  if game.done: break

  if not stop:
    game.update(dt)
    
    
    distances = []
    changed = []
    for s in soldiers:
      distances.append(s.target_position.get_dist_sqrd(s.body.position)**0.5)
      if  distances[-1] < 5 and len(s.trajectory)>1: 
        s.trajectory.pop(0)
        s.target_position, s.coord = s.trajectory[0]
        s.changed_target = True
      changed.append(s.changed_target) 
    
    if sum(changed) == unit.n:
      for s in soldiers: s.changed_target = False
      changed = [False] * unit.n
    
    t = np.array(distances)[True != np.array(changed)].mean() / s.base_speed
    min_dist = np.min(np.array(distances)[True != np.array(changed)])
    for i,s in enumerate(soldiers):
      
      if s.changed_target:
        cur_speed = s.base_speed / 4 # min_dist / t
      else:
        cur_speed = distances[i] / t
      if s.target_position.get_dist_sqrd(s.body.position) < 2: continue
      s.body.angle = (s.target_position - s.body.position).angle
      s.move(cur_speed, s.unit.LATERAL_NOISE_MULTIPLIER)
        
        
  game.draw(DEBUG)
  unit.draw(DEBUG)

  pygame.display.flip()
  game.clock.tick(game.fps)

  if game.record:
    arr = pygame.surfarray.array3d(game.screen).swapaxes(1, 0)
    game.video.append(arr.astype(np.uint8))





# %%


import matplotlib.pyplot as plt

get_formation = unit.formation.get_formation

posvc2 =  Vec2d(list(unit.pos+np.array([100,100])))
angle = (Vec2d(list(pos))-posvc2).perpendicular().angle
pos = np.array(list(posvc2))
n_ranks = unit.n_ranks
n = unit.n
size, dist = unit.soldier_size_dist
f1,_ = get_formation(pos, angle, n_ranks, n, size, dist)


posvc2 =  Vec2d(list(pos+np.array([300,100])))
angle = (Vec2d(list(pos))-posvc2).perpendicular().angle
pos = np.array(list(posvc2))
n_ranks = unit.n_ranks
n = unit.n
size, dist = unit.soldier_size_dist
f2,_ = get_formation(pos, angle, n_ranks, n, size, dist)


f1 = traj_form[0]
f2 = traj_form[1]

# %% 


start = np.array(unit.get_soldiers_pos(False))


st_f1 = pairwise_distances(start, f1)
f1_f2 = pairwise_distances(f1, f2)


_, col_s1f1 = linear_sum_assignment(st_f1)
_, col_f1f2 = linear_sum_assignment(f1_f2)

for i,j in enumerate(col_s1f1):
  plt.plot([start[i][0], f1[j][0]], [start[i][1], f1[j][1]], color = "tab:blue")
for i,j in enumerate(col_f1f2):
  plt.plot([f1[i][0], f2[j][0]], [f1[i][1], f2[j][1]], color = "tab:blue")


start_f2 = np.vstack((st_f1,f1_f2))
row_ind, col_s1f2 = linear_sum_assignment(start_f2)





plt.scatter(*start.T, color = "tab:green")
plt.scatter(*f1.T, color = "tab:orange")
plt.scatter(*f2.T, color = "red")
