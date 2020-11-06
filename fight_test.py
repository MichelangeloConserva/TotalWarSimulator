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


record = True
DEBUG = False

def unit_selection():
  global selected_unit
  
  mouse_pos = game.get_mouse_pos(False)
  for u in units:
    inf_hull, _ = u.formation.get_hulls()  
    if inf_hull.contains(Point(*mouse_pos)):
      if not selected_unit is None: selected_unit.is_selected = False
      u.is_selected = True
      selected_unit = u
      return
  if not selected_unit is None: selected_unit.is_selected = False
  selected_unit = None

def unit_attack():
  global selected_unit
  
  if selected_unit is None: return
  
  mouse_pos = game.get_mouse_pos(False)
  for e in selected_unit.enemies:
    inf_hull, _ = e.formation.get_hulls()  
    if inf_hull.contains(Point(*mouse_pos)):
      selected_unit.enemy_target = e
      return True
  return False


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
        
        if not unit_attack():
          game.record_mouse = True
          game.mouse_traj = [game.get_mouse_pos(False)]
        
      if event.button == 1:
        unit_selection()
        
    if event.type == pygame.MOUSEBUTTONUP:
      if event.button == 3:
        game.record_mouse = False
        
    if game.record_mouse:
        game.mouse_traj.append(game.get_mouse_pos(False))
        
  return stop

def hull_calc(units):
  units_fight_hulls = []
  for u in units:
    _, fight_hull = u.formation.get_hulls()
    units_fight_hulls.append((u,fight_hull))
  return units_fight_hulls


from shapely.ops import nearest_points, split
from shapely.geometry import Polygon, mapping, MultiPoint, LineString
from shapely.affinity import scale

def wrapping_points(start, dir1, dir2, closest, n, size):
    coords = [start]
    for i in range(n): # Number of soldiers in the first line
      coords.append(start + dir2 * i * (size))
      coords.append(start + dir1 * i * (size))
    argsorted = np.argsort(((np.array(coords) - closest)**2).sum(1)**0.5)[:20]
    return np.array(coords)[argsorted].tolist() , argsorted

def get_good_points(u_def_rect, nps):
  points = []
  distances = []
  for c in list(u_def_rect.coords)[:-1]:
    points.append(np.array(c))
    distances.append(Point(c).distance(nps))
  return [points[i] for i in np.argsort(distances)[:3]]

def get_directions(good_points):
  start = good_points[0]
  dir1 = good_points[1] - good_points[0]
  dir1 = dir1 / sum(dir1**2)**0.5
  dir2 = good_points[2] - good_points[0]
  dir2 = dir2 / sum(dir2**2)**0.5
  return start + dir1, dir1, dir2

def attacking_formation(u_att, u_def):
  
  size, dist = u_att.soldier_size_dist
  
  x = [0]*20+[1]*20+[2]*20+[3]*20+[4]*20
  rank_ind = {k:x[k] for k in range(len(x))}
  
  u_att_rect = MultiPoint(u_att.get_soldiers_pos(False)).minimum_rotated_rectangle
  u_def_rect = MultiPoint(u_def.get_soldiers_pos(False)).minimum_rotated_rectangle.boundary
  
  nps = nearest_points(u_att_rect, u_def_rect)
  good_points = get_good_points(u_def_rect, nps[1])
  start, dir1, dir2 = get_directions(good_points)
  closest = np.array(u_att.pos).reshape(-1,2)
  
  formation , argsorted = wrapping_points(start, dir1, dir2, closest, 20, size+dist)

  for j in range(1,u_att.n_ranks):
    
    start = good_points[0] - dir1 * (j) * (size+dist) - dir2 * j * (size+dist)
    # cur_row , argsorted = wrapping_points(start, dir1, dir2, closest, 20+j, size)
    cur_row , argsorted = wrapping_points(start, dir1, dir2, closest, 20, size+dist)
    
    if len(cur_row) + len(formation) > u_att.n: 
      f = u_att.n - len(formation)
      formation += cur_row[:f]
    else: formation += cur_row
    
  return np.array(formation), rank_ind



def melee_checker():
  global stop 
  
  units_1_fight_hulls, units_2_fight_hulls = hull_calc(units_1), hull_calc(units_2)
  for u1,fight_hull1 in units_1_fight_hulls:
    for u2,fight_hull2 in units_2_fight_hulls: 
      if fight_hull1.intersects(fight_hull2):
        
        if not u1.is_fighting and not u2.is_fighting:
          
          
          unit_pos = u1.pos
          other_pos = u2.pos
          
          final_angle = Vec2d(list(unit_pos-other_pos)).perpendicular().angle
          formation, rank_ind = u1.get_formation_at(unit_pos*0.3+0.7*other_pos, final_angle)   
          for s in u1.soldiers: s.reset_traj()
          u1.set_formation_(formation, rank_ind)
          u1.update_soldiers_trajectory()  
          
          final_angle = Vec2d(list(-unit_pos+other_pos)).perpendicular().angle
          formation, rank_ind = u2.get_formation_at(unit_pos*0.7+0.3*other_pos, final_angle)   
          for s in u2.soldiers: s.reset_traj()
          u2.set_formation_(formation, rank_ind)
          u2.update_soldiers_trajectory()             
          
          u1.is_fighting = u2.is_fighting = True
        
        
        elif not u1.is_fighting and u2.is_fighting: 
        
          for s in u1.soldiers: s.reset_traj()
          formation, rank_ind = attacking_formation(u1, u2)
          u1.set_formation_(formation, rank_ind)
          u1.update_soldiers_trajectory()
          
          u1.is_fighting = True
        
        # global to_draw
        # # to_draw = formation
        # to_draw = [list(s.trajectory[0][0]) for s in u1.soldiers]
        
        # stop = True
        # return



game = Game(record)
dt = 1 / game.fps

units_1 = []
for i in [300,700]: units_1.append(Unit(game, (i, 600), 0, RED, 1))
units_2 = []
for i in [230,600]: units_2.append(Unit(game, (i, 350), np.pi, GREEN, 2))
for u in units_1: u.enemies = units_2
for u in units_2: u.enemies = units_1
units = units_1 + units_2
selected_unit = None


to_draw = []
stop = False
while not game.done:
  game.screen.fill(WHITE)
  stop = game_loop(game, stop)
  if game.done: break


  melee_checker()

  if not stop:
    game.update(dt)
    
    for u in units:
      u.update(dt)

    if not game.record_mouse and len(game.mouse_traj) > 0:
      
      if len(game.mouse_traj) > 3:
        trajectory = np.array(game.mouse_traj)
        trajectory = trajectory + np.random.randn(len(trajectory), 2)*0.0001
        selected_unit.set_spline_formation_trajectory(None, None, trajectory, True)
        game.mouse_traj = []
        
  game.draw(DEBUG)
  for u in units: u.draw(DEBUG)
  
  if not selected_unit is None:
    traj_form = selected_unit.traj
    for i in range(len(traj_form)-1):
      p1 = to_pygame(traj_form[i], game.screen)
      p2 = to_pygame(traj_form[i+1], game.screen)
      pygame.draw.aalines(game.screen, (0,0,0), False, [p1,p2])
  
  
  
  # for t in to_draw:
  #   p = to_pygame(t, game.screen)
  #   pygame.draw.circle(game.screen, (0,0,0), p, 3)
  # if selected_unit:
  #   for s in selected_unit.soldiers:
  #     try:
  #       t = list(s.trajectory[0][0])
  #       p = to_pygame(t, game.screen)
  #       pygame.draw.circle(game.screen, (0,0,0), p, 3)
  #     except:
  #       pass
  
  pygame.display.flip()
  game.clock.tick(game.fps)

  if game.record:
    arr = pygame.surfarray.array3d(game.screen).swapaxes(1, 0)
    game.video.append(arr.astype(np.uint8))






PROVE


# %%
from shapely.geometry import Polygon, mapping
from scipy.spatial import ConvexHull


def get_surrounding_points(defender, attacker):
  
  def_pos = np.array(defender.get_soldiers_pos(False))
  
  hull_def = ConvexHull(def_pos)
  hull_def = Polygon(def_pos[hull_def.vertices])
  def_coord = np.array(mapping(hull_def)["coordinates"]).astype(int).reshape(-1,2)
  
  size, dist = attacker.soldier_size_dist
  new_coors = []
  for prev, nex in zip(def_coord[:-1],def_coord[1:]):
    distance = sum((prev-nex)**2)**0.5
    T = int(np.floor((distance  + dist + size) / (size)))
    # print(T)
    
    for t in np.linspace(0,1,T):
      new_coors.append(list(prev * (1-t) + nex * t))
  
  return np.unique(new_coors, axis = 0)
  
  





units_1_fight_hulls, units_2_fight_hulls = hull_calc(units_1), hull_calc(units_2)

for u1,fight_hull1 in units_1_fight_hulls:
  for u2,fight_hull2 in units_2_fight_hulls: 
    if fight_hull1.intersects(fight_hull2):
      Unit1, Unit2 = u1, u2 



Unit1_pos = np.array(u1.get_soldiers_pos(False))
Unit2_pos = np.array(u2.get_soldiers_pos(False))

hull_defender = ConvexHull(Unit2_pos)
hull_defender = Polygon(Unit2_pos[hull_defender.vertices])

size, dist = u1.soldier_size_dist
def_coord = np.array(mapping(hull_defender)["coordinates"]).astype(int).reshape(-1,2)





# Prova 1

for u1 in units_2:
  for u2 in units_1:
    
    for u11 in units_2:
      for u22 in units_1:
        
        plt.scatter(*np.array(u11.get_soldiers_pos(False)).T, color = "green", alpha = 0.1)
        plt.scatter(*np.array(u22.get_soldiers_pos(False)).T, color = "red", alpha = 0.1)
        
    us_pos = np.array(u2.get_soldiers_pos(False))
    hull_defender = ConvexHull(us_pos)
    hull_defender = Polygon(us_pos[hull_defender.vertices])
    def_coord = np.array(mapping(hull_defender)["coordinates"]).astype(int).reshape(-1,2)

    new_coors = []
    for prev, nex in zip(def_coord[:-1],def_coord[1:]):
      distance = sum((prev-nex)**2)**0.5
      T = int(np.floor((distance  + dist + size) / (size)))
      print(T)
      
      for t in np.linspace(0,1,T):
        new_coors.append(list(prev * (1-t) + nex * t))
    
    new_coors = np.unique(new_coors, axis = 0)
    
    
    front_line = np.array([list(s.body.position) for s in u1.soldiers if s.coord[0] == u1.n_ranks-1])
    
    
    
    plt.scatter(*np.array(u1.get_soldiers_pos(False)).T, color = "green", alpha = 0.5)
    plt.scatter(*np.array(u2.get_soldiers_pos(False)).T, color = "red", alpha = 0.5)
    plt.scatter(*new_coors.T)
    plt.scatter(*front_line.T, color = "yellow")
    plt.show()
    print("-----")
      
      


# Prova 3
from shapely.ops import nearest_points, split
from shapely.geometry import Polygon, mapping, MultiPoint, LineString
from shapely.affinity import scale


u1_pos = np.array(u1.get_soldiers_pos(False))
hull_attacker = ConvexHull(u1_pos)
hull_attacker = Polygon(us_pos[hull_attacker.vertices]).minimum_rotated_rectangle.boundary

u2_pos = np.array((u2.get_soldiers_pos(False)))
hull_defender = ConvexHull(u2_pos)
hull_defender = Polygon(u2_pos[hull_defender.vertices])

nps = nearest_points(hull_attacker, hull_defender)





plt.scatter(*np.array(u1.get_soldiers_pos(False)).T, color = "green", alpha = 0.5)
plt.scatter(*np.array(u2.get_soldiers_pos(False)).T, color = "red", alpha = 0.5)
plt.scatter(nps[0].x, nps[0].y, color = "green")
plt.scatter(nps[1].x, nps[1].y, color = "red")





u1_pos = MultiPoint(u2.get_soldiers_pos(False))
u2_pos = MultiPoint(u1.get_soldiers_pos(False))

u2_rect = u2_pos.minimum_rotated_rectangle.boundary

nps = nearest_points(u1_pos, u2_rect)

x0,y0 = u2_rect.coords.xy
u2_rect = LineString([(nps[1].x, nps[1].y)] + [(x,y) for x,y in zip(x0[:-1],y0[:-1])])


num_points0 = 10
new_points = [u2_rect.interpolate(i*size) for i in range(num_points0)]
MultiPoint(new_points + u2.get_soldiers_pos(False))



plt.scatter(*np.array(u1.get_soldiers_pos(False)).T, color = "green", alpha = 0.5)
plt.scatter(*np.array(u2.get_soldiers_pos(False)).T, color = "red", alpha = 0.5)
plt.scatter(nps[0].x, nps[0].y, color = "green")
plt.scatter(nps[1].x, nps[1].y, color = "red")








u1_pos = MultiPoint(u1.get_soldiers_pos(False))
u2_pos = MultiPoint(u2.get_soldiers_pos(False))

u1_rect = u1_pos.minimum_rotated_rectangle
u2_rect = u2_pos.minimum_rotated_rectangle

nps = nearest_points(u1_rect, u2_rect)
scale_factor = nps[0].distance(nps[1]) + 10 * size
u1_rect = u1_rect.buffer(scale_factor)

u1_rect - u2_rect

u2_rect.intersection(u1_rect)





u1_pos = MultiPoint(u1.get_soldiers_pos(False))
u2_pos = MultiPoint(u2.get_soldiers_pos(False))

u1_rect = u1_pos.minimum_rotated_rectangle
u2_rect = u2_pos.minimum_rotated_rectangle.boundary


num_points0 = int(u2_rect.length // (size + dist))
new_coords = []
for i in range(num_points0):
  cur = u2_rect.interpolate(i*(size+dist))
  new_coords.append((cur.x,cur.y))

plt.scatter(*np.array(u1.get_soldiers_pos(False)).T, color = "green", alpha = 0.5)
plt.scatter(*np.array(u2.get_soldiers_pos(False)).T, color = "red", alpha = 0.5)
plt.scatter(*np.array(new_coords).T, color = "black")








# %%
    
for u1 in units_2:
  for u2 in units_1:
        
    # plt.scatter(*np.array(u1.get_soldiers_pos(False)).T, color = "green", alpha = 0.1)
    # plt.scatter(*np.array(u2.get_soldiers_pos(False)).T, color = "red", alpha = 0.1)

    u1_pos = MultiPoint(u1.get_soldiers_pos(False))
    u2_pos = MultiPoint(u2.get_soldiers_pos(False))
    
    u1_rect = u1_pos.minimum_rotated_rectangle
    u2_rect = u2_pos.minimum_rotated_rectangle.boundary
    
    # nps = nearest_points(u1_rect, u2_rect)
    nps = nearest_points(Point(u1.pos), u2_rect)
    
    # plt.scatter(*np.array(list(nps[0].coords)[0]).T, color = "green")
    # plt.scatter(*np.array(list(nps[1].coords)[0]).T, color = "red")
    # plt.show()
    
    
    points = []
    distances = []
    for c in list(u2_rect.coords)[:-1]:
      points.append(np.array(c))
      distances.append(Point(c).distance(nps[1]))
    
    good_points = [points[i] for i in np.argsort(distances)[:3]]
  

    start = good_points[0]
    dir1 = good_points[1] - good_points[0]
    dir1 = dir1 / sum(dir1**2)**0.5
    dir2 = good_points[2] - good_points[0]
    dir2 = dir2 / sum(dir2**2)**0.5
    
    
    if np.diff(np.sort(distances)[1:3]) < 10:
      new_coords = [start]
      for i in range(20): # Number of soldiers in the first line
        new_coords.append(start + dir2 * i * (size))
        new_coords.append(start + dir1 * i * (size))
    else:
      new_coords = []
      for i in range(20): # Number of soldiers in the first line
        new_coords.append(start + dir1 * i * (size))
        new_coords.append(start + dir2 * i * (size))



    new_coords = np.array(new_coords)
    closest = np.array(u1.pos).reshape(-1,2)
    
    argsorted = np.argsort(((new_coords - closest)**2).sum(1)**0.5)[:20]
    
    final = new_coords[argsorted]
    for ss in final:
      plt.text(int(ss[0]), int(ss[1]), str(0))
    
    
    argsorted = argsorted[::-1]
    final = final[::-1]
    
    formation = final.tolist()
    for j in range(1,u1.n_ranks):
      cur_row = []
      
      for index,a_s in enumerate(argsorted):
        if a_s % 2 == 0:
          cur_row.append(final[index] - dir2 * size * j)
        else:
          cur_row.append(final[index] - dir1 * size * j)
      
      cur_row.append(start - dir1 * size * j - dir2 * size * j)
      for jj in range(1,j):
        for ii in range(j,j+1):
          cur_row.append(start - dir1 * size * jj - dir2 * size * ii)
          cur_row.append(start - dir1 * size * ii - dir2 * size * jj)
      
      # final = cur_row
      if len(cur_row) + len(formation) > u1.n: 
        f = u1.n - len(formation)
        formation += cur_row[:f]
      else:
        formation += cur_row
      
      for ss in cur_row:
        plt.text(int(ss[0]), int(ss[1]), str(j))
    
    assert len(formation) == 100
    formation = np.array(formation)
    
    
    plt.scatter(*np.array(u1.get_soldiers_pos(False)).T, color = "green", alpha = 0.5)
    plt.scatter(*np.array(u2.get_soldiers_pos(False)).T, color = "red", alpha = 0.5)
    plt.scatter(*new_coords.T, color = "yellow")
    # plt.scatter(*final.T, color = "orange")
    # plt.scatter(*formation.T, color = "black")
    # plt.scatter(*np.array(cur_row).T, color = "orange")
    
    # plt.scatter(*start, color = "black")
    # plt.scatter(*good_points[1], color = "black")
    # plt.scatter(*good_points[2], color = "black")
    plt.show()
  
  #   break
  # break


# %%    VERY GOOD 

def wrapping_points(start, dir1, dir2, closest, n, size):
    coords = [start]
    for i in range(n): # Number of soldiers in the first line
      coords.append(start + dir2 * i * (size))
      coords.append(start + dir1 * i * (size))
    argsorted = np.argsort(((np.array(coords) - closest)**2).sum(1)**0.5)[:20]
    return np.array(coords)[argsorted].tolist() , argsorted


for u1 in units_2:
  for u2 in units_1:
        
    plt.scatter(*np.array(u1.get_soldiers_pos(False)).T, color = "green", alpha = 0.1)
    plt.scatter(*np.array(u2.get_soldiers_pos(False)).T, color = "red", alpha = 0.1)
    plt.scatter(*attacking_formation(u1, u2).T, color = "black")
    plt.show()


    u1_pos = MultiPoint(u1.get_soldiers_pos(False))
    u2_pos = MultiPoint(u2.get_soldiers_pos(False))
    
    u1_rect = u1_pos.minimum_rotated_rectangle
    u2_rect = u2_pos.minimum_rotated_rectangle.boundary
    
    nps = nearest_points(u1_rect, u2_rect)
    # nps = nearest_points(Point(u1.pos), u2_rect)
    
    # plt.scatter(*np.array(list(nps[0].coords)[0]).T, color = "green")
    # plt.scatter(*np.array(list(nps[1].coords)[0]).T, color = "red")
    # plt.show()
    
    
    points = []
    distances = []
    for c in list(u2_rect.coords)[:-1]:
      points.append(np.array(c))
      distances.append(Point(c).distance(nps[1]))
    good_points = [points[i] for i in np.argsort(distances)[:3]]

    start = good_points[0]
    dir1 = good_points[1] - good_points[0]
    dir1 = dir1 / sum(dir1**2)**0.5
    dir2 = good_points[2] - good_points[0]
    dir2 = dir2 / sum(dir2**2)**0.5
    start = start + dir1
    closest = np.array(u1.pos).reshape(-1,2)
    
    final , argsorted = wrapping_points(start, dir1, dir2, closest, 20, size)
    print(sum(argsorted % 2 == 0))
    for ss in final:
      plt.text(int(ss[0]), int(ss[1]), str(0))
    
    formation = final
    for j in range(1,u1.n_ranks):
      
      start = good_points[0] - dir1 * (j) * (size) - dir2 * j * (size)
      cur_row , argsorted = wrapping_points(start, dir1, dir2, closest, 20+j, size)
      
      # final = cur_row
      if len(cur_row) + len(formation) > u1.n: 
        f = u1.n - len(formation)
        formation += cur_row[:f]
      else:
        formation += cur_row
      
      for ss in cur_row:
        plt.text(int(ss[0]), int(ss[1]), str(j))
    
    # assert len(formation) == 100
    formation = np.array(formation)
    
    
    plt.scatter(*np.array(u1.get_soldiers_pos(False)).T, color = "green", alpha = 0.5)
    plt.scatter(*np.array(u2.get_soldiers_pos(False)).T, color = "red", alpha = 0.5)
    # plt.scatter(*new_coords.T, color = "yellow")
    # plt.scatter(*final.T, color = "orange")
    # plt.scatter(*formation.T, color = "black")
    # plt.scatter(*np.array(cur_row).T, color = "orange")
    plt.scatter(*good_points[0], color = "black")
    # plt.scatter(*good_points[1], color = "black")
    # plt.scatter(*good_points[2], color = "black")
    plt.show()
  
  #   break
  # break




# %% 


for u1 in units_2:
  for u2 in units_1:
        
    # plt.scatter(*np.array(u1.get_soldiers_pos(False)).T, color = "green", alpha = 0.1)
    # plt.scatter(*np.array(u2.get_soldiers_pos(False)).T, color = "red", alpha = 0.1)

    u1_pos = MultiPoint(u1.get_soldiers_pos(False))
    u2_pos = MultiPoint(u2.get_soldiers_pos(False))
    
    u1_rect = u1_pos.minimum_rotated_rectangle
    u2_rect = u2_pos.minimum_rotated_rectangle.boundary
    
    # nps = nearest_points(u1_rect, u2_rect)
    nps = nearest_points(Point(u1.pos), u2_rect)
    
    # plt.scatter(*np.array(list(nps[0].coords)[0]).T, color = "green")
    # plt.scatter(*np.array(list(nps[1].coords)[0]).T, color = "red")
    # plt.show()
    
    
    points = []
    distances = []
    for c in list(u2_rect.coords)[:-1]:
      points.append(np.array(c))
      distances.append(Point(c).distance(nps[1]))
    
    good_points = [points[i] for i in np.argsort(distances)[:3]]
  

    start = good_points[0]
    dir1 = good_points[1] - good_points[0]
    dir1 = dir1 / sum(dir1**2)**0.5
    dir2 = good_points[2] - good_points[0]
    dir2 = dir2 / sum(dir2**2)**0.5
    
    
    new_coords = []
    for i in range(20): # Number of soldiers in the first line
      new_coords.append(start + dir1 * i * (size))
      new_coords.append(start + dir2 * i * (size))


    new_coords = np.array(new_coords)
    closest = np.array(u1.pos).reshape(-1,2)
    _argsorted = np.argsort(((new_coords - closest)**2).sum(1)**0.5)
    argsorted = _argsorted[:20]
    final = new_coords[argsorted]
    
    final = np.vstack((final,new_coords[_argsorted[:24]] - size * (dir1 + dir2)))
    
    # for ss in final:
    #   plt.text(int(ss[0]), int(ss[1]), str(0))
    
    
    
    
    plt.scatter(*np.array(u1.get_soldiers_pos(False)).T, color = "green", alpha = 0.5)
    plt.scatter(*np.array(u2.get_soldiers_pos(False)).T, color = "red", alpha = 0.5)
    # plt.scatter(*new_coords.T, color = "yellow")
    plt.scatter(*final.T, color = "orange")
    # plt.scatter(*formation.T, color = "black")
    # plt.scatter(*np.array(cur_row).T, color = "orange")
    
    plt.scatter(*start, color = "black")
    # plt.scatter(*good_points[1], color = "black")
    # plt.scatter(*good_points[2], color = "black")
    plt.show()
  
  #   break
  # break


# %%



plt.scatter(*np.array(u1.get_soldiers_pos(False)).T, color = "green", alpha = 0.1)
plt.scatter(*np.array(u2.get_soldiers_pos(False)).T, color = "red", alpha = 0.1)


u1_pos = MultiPoint(u1.get_soldiers_pos(False)).convex_hull.buffer(-6)
u2_pos = MultiPoint(u2.get_soldiers_pos(False)).convex_hull.buffer(-6)
total = u1_pos.union(u2_pos)

# C = np.zeros((len(u1.soldiers),len(u2.soldiers))) + 1e32

# for i,s1 in enumerate(u1.soldiers):

for j,s2 in enumerate(u2.soldiers):
  
  conn = LineString((list(s1.body.position),list(s2.body.position)))
  print(conn.intersects(total))
  
  
  

  
# %% 
















# %% 




# Prova 2

def get_attack_formation(attacker_unit, defender_unit):
  
  Unit2_pos = np.array(defender_unit.get_soldiers_pos(False))
  
  hull_defender = ConvexHull(Unit2_pos)
  hull_defender = Polygon(Unit2_pos[hull_defender.vertices])
  
  size, dist = attacker_unit.soldier_size_dist
  def_coord = np.array(mapping(hull_defender)["coordinates"]).astype(int).reshape(-1,2)
  
  
  front_line = np.array([list(s.body.position) for s in attacker_unit.soldiers if s.coord[0] == attacker_unit.n_ranks-1])
  border_soldiers = np.array([list(s.body.position) for s in attacker_unit.soldiers\
                              if s.coord[0] == attacker_unit.n_ranks-1 and (s.coord[1] == 0 or s.coord[1] == 19)])
    
  min_dist_solds = []
  mins = []
  for dc in def_coord[:-1]:
    mins.append(min(((dc - front_line)**2).sum(1)**0.5))
    min_dist_solds.append(np.argmin(((dc - front_line)**2).sum(1)**0.5))
  closest_def = def_coord[np.argmin(mins)]
  min_dist_sold = front_line[min_dist_solds[np.argmin(mins)]]
  
  d1 = sum((border_soldiers[0] - min_dist_sold)**2)**0.5
  d2 = sum((border_soldiers[0] - border_soldiers[1])**2)**0.5
  
  alpha = d1 / d2
  
  # alpha = (min_dist_sold -  border_soldiers[1]) / (border_soldiers[0] - border_soldiers[1]) 
  # min_dist_sold = alpha * border_soldiers[0] + (1-alpha) * border_soldiers[1]
  
  n_dir1 = int(20*alpha)
  n_dir2 = 20 - n_dir1
  
  start, end1, end2 = closest_def, def_coord[(np.argmin(mins) + 1) % len(mins) ], def_coord[(np.argmin(mins) - 1) % len(mins) ]
  
  di1 = end1 - start
  di1 = (di1 / sum(di1**2)**0.5) * (size)
  
  di2 = end2 - start
  di2 = (di2 / sum(di2**2)**0.5) * (size)
  
  n = attacker_unit.n
  n_ranks = attacker_unit.n_ranks
  
  formation = []
  rank_ind = {}
  k = 0
  for j in range(n_ranks):
    
    row_pos = [list(start + di1 * i - di2 * j) for i in range(-j, n_dir1)] +\
              [list(start + di2 * i - di1 * j) for i in range(1-j,n_dir2)]
  
    if len(row_pos) + len(formation) > n: 
      formation += row_pos[::-1][:(n - len(formation))]  
    
    else:                                 
      formation += row_pos
  
    for _ in range(len(row_pos)):
      rank_ind[k] = j
      k += 1
  return formation, rank_ind




for u1 in units_1:
  for u2 in units_2:
    
    for u11 in units_1:
      for u22 in units_2:
        
        plt.scatter(*np.array(u11.get_soldiers_pos(False)).T, color = "green", alpha = 0.1)
        plt.scatter(*np.array(u22.get_soldiers_pos(False)).T, color = "red", alpha = 0.1)
        
    formation, _ = get_attack_formation(u1, u2)
    
    plt.scatter(*np.array(u1.get_soldiers_pos(False)).T, color = "green", alpha = 0.5)
    plt.scatter(*np.array(u2.get_soldiers_pos(False)).T, color = "red", alpha = 0.5)
    plt.scatter(*np.array(formation).T, color = "black")
    plt.show()
  
  
  
  
  
  
attacker_unit = u1 = units_1[0]
defender_unit = u2 = units_2[1]


Unit2_pos = np.array(defender_unit.get_soldiers_pos(False))

hull_defender = ConvexHull(Unit2_pos)
hull_defender = Polygon(Unit2_pos[hull_defender.vertices])

size, dist = attacker_unit.soldier_size_dist
def_coord = np.array(mapping(hull_defender)["coordinates"]).astype(int).reshape(-1,2)


front_line = np.array([list(s.body.position) for s in attacker_unit.soldiers if s.coord[0] == attacker_unit.n_ranks-1])
border_soldiers = np.array([list(s.body.position) for s in attacker_unit.soldiers\
                            if s.coord[0] == attacker_unit.n_ranks-1 and (s.coord[1] == 0 or s.coord[1] == 19)])
  
min_dist_solds = []
mins = []
for dc in def_coord[:-1]:
  mins.append(min(((dc - front_line)**2).sum(1)**0.5))
  min_dist_solds.append(np.argmin(((dc - front_line)**2).sum(1)**0.5))
closest_def = def_coord[np.argmin(mins)]
min_dist_sold = front_line[min_dist_solds[np.argmin(mins)]]

d1 = sum((border_soldiers[0] - min_dist_sold)**2)**0.5
d2 = sum((border_soldiers[0] - border_soldiers[1])**2)**0.5

alpha = d1 / d2



# alpha = (min_dist_sold -  border_soldiers[1]) / (border_soldiers[0] - border_soldiers[1]) 
# min_dist_sold = alpha * border_soldiers[0] + (1-alpha) * border_soldiers[1]

n_dir1 = int(20*alpha)
n_dir2 = 20 - n_dir1

start, end1, end2 = closest_def, def_coord[(np.argmin(mins) + 1) % len(mins) ], def_coord[(np.argmin(mins) - 1) % len(mins) ]

di1 = end1 - start
di1 = (di1 / sum(di1**2)**0.5) * (size)

di2 = end2 - start
di2 = (di2 / sum(di2**2)**0.5) * (size)

n = attacker_unit.n
n_ranks = attacker_unit.n_ranks

formation = []
rank_ind = {}
k = 0
for j in range(n_ranks):
  
  row_pos = [list(start + di1 * i - di2 * j) for i in range(-j, n_dir1)] +\
            [list(start + di2 * i - di1 * j) for i in range(1-j,n_dir2)]

  if len(row_pos) + len(formation) > n: 
    formation += row_pos[::-1][:(n - len(formation))]  
  
  else:                                 
    formation += row_pos

  for _ in range(len(row_pos)):
    rank_ind[k] = j
    k += 1

plt.scatter(*min_dist_sold, color = "green")
plt.scatter(*np.array(u1.get_soldiers_pos(False)).T, color = "green", alpha = 0.5)
plt.scatter(*np.array(u2.get_soldiers_pos(False)).T, color = "red", alpha = 0.5)
plt.scatter(*np.array(formation).T, color = "black")
plt.show()








u2_pos = u2.pos
u1_pos = u1.pos
dist = max([s.body.position.get_dist_sqrd(u2_pos) for s in u2.soldiers])**0.5


attack_position = u2_pos + (u1_pos - u2_pos).normalized() * dist * 0.8
af, _ = u1.get_formation_at(attack_position, (u2.pos - u1.pos).perpendicular().angle)

hull_attacker = ConvexHull(af)
hull_attacker = Polygon(af[hull_attacker.vertices])


hull_defender = ConvexHull(Unit2_pos)
hull_defender = Polygon(Unit2_pos[hull_defender.vertices])




size, dist = u1.soldier_size_dist


def_coord = np.array(mapping(hull_defender)["coordinates"]).astype(int).reshape(-1,2)


length_front_line = sum([s.coord[0] == u1.n_ranks-1 for s in u1.soldiers])

new_coors = []
for prev, nex in zip(def_coord[:-1],def_coord[1:]):
  distance = sum((prev-nex)**2)**0.5
  T = int(np.floor((distance  + dist + size) / (size)))
  print(T)
  
  for t in np.linspace(0,1,T):
    new_coors.append(list(prev * (1-t) + nex * t))
new_coors = np.array(new_coors)

plt.scatter(*Unit1_pos.T, color = "green")
plt.scatter(*Unit2_pos.T, color = "red")
plt.scatter(*np.array(af).T, color = "blue")
plt.scatter(*new_coors.T)
plt.scatter(*def_coord.T, color = "black")




prova = (hull_attacker - hull_defender)

new_vertices = []

att_coord = np.array(mapping(hull_attacker)["coordinates"]).astype(int).reshape(-1,2)
new_coord = np.array(mapping(prova)["coordinates"]).astype(int).reshape(-1,2)

for v in new_coord:
  if v not in att_coord: new_vertices.append(v)
new_vertices = np.array(new_vertices)

plt.scatter(*Unit2_pos.T, color = "red")
plt.scatter(*np.array(af).T, color = "blue")
plt.scatter(*u1_pos, color = "blue")
plt.scatter(*att_coord.T, color = "orange")
plt.scatter(*new_vertices.T, color = "yellow")








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