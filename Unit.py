import numpy as np
import matplotlib.pyplot as plt
import pygame
import pymunk

from pymunk.vec2d import Vec2d
from pymunk.pygame_util import to_pygame, from_pygame
from scipy.spatial import ConvexHull, convex_hull_plot_2d
from scipy import interpolate
from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment

from utils.pygame_utils import draw_text
from utils.enums import Role, UnitType, UnitState
from utils.colors import BLACK, BLUE, GREEN
from Formation import Formation
from Soldier import Soldier





class Unit:

  soldier = Soldier
  role = UnitType.INFANTRY
  start_n = 100
  start_ranks = 5
  dist = 5
  FORCE_MULTIPLIER = 10
  dumping_intra = 1
  damping_def = 0.5
  stifness_intra = FORCE_MULTIPLIER * 55 * soldier.mass
  stifness_def = FORCE_MULTIPLIER * 65 * soldier.mass
  LATERAL_NOISE_MULTIPLIER = FORCE_MULTIPLIER
  ATTACKING_MULTIPLIERS = 0.6
  melee_range = soldier.radius * 2 + soldier.dist
  ratio = 4  # This means that we want the number of soldiers in a line be 4 time
  ANGLE_TH = 50
  SPLINE_DISTANCE_POINT = 50
  
  
  @property
  def soldier_size_dist(self): return self.soldier.radius * 2, self.soldier.dist
  @property
  def n(self):                 return len(self.soldiers)
  @property
  def pos(self): return sum(self.get_soldiers_pos()) / len(self.soldiers)
  @property
  def formation(self):
    if not hasattr(self, "_formation"):
      self._formation = Formation(self, self.melee_range, self.ratio)
    return self._formation
  @property
  def pos_direction(self):
    n = 0
    center = Vec2d()
    first_line_pos = Vec2d()
    for s in self.soldiers:
      if s.coord[0] == self.n_ranks-1:
        first_line_pos += s.body.position
        n += 1
      center += s.body.position / self.n
    first_line_pos /= n
    return center, (first_line_pos-center).normalized()
    
  def get_soldiers_pos(self, vec2d=True):
    typ = Vec2d if vec2d else list
    return [typ(s.body.position) for s in self.soldiers]
  
  def check_dir_angle(self, dir_angle): return  dir_angle > self.ANGLE_TH
  
  def get_n_points(self, length): return max(int(length/self.SPLINE_DISTANCE_POINT),2)
  
  def get_spline(self, trajectory, show = False):
    if show:
      plt.scatter(*trajectory.T)
      
    length = sum((np.diff(trajectory,axis=0)**2).sum(1)**0.5)
    f, u = interpolate.splprep(trajectory.T.tolist(), s=0, k=2)
    inter_points = np.linspace(0, 1, self.get_n_points(length))
    xint, yint = interpolate.splev(inter_points, f)
    trajectory = [Vec2d(list(t)) for t in np.vstack((xint, yint)).T]
    
    directions = np.diff(trajectory,axis=0)
    directions = (directions / ((directions**2).sum(1)**0.5).reshape(-1,1)) #* int(length/80)
    
    if show:
      for i in range(len(xint)-1):
        plt.plot((xint[i],xint[i]+directions[i][0]), 
                 (yint[i],yint[i]+directions[i][1]), color = "red")
  
    return trajectory, [Vec2d(list(d.round(2))) for d in directions]
  
  @staticmethod
  def get_cur_traj(trajectory, directions, i, last_dir, final_dir):
    cur_pos = Vec2d(trajectory[i])
    
    if i < len(directions): cur_dir = directions[i]
    else:
      if final_dir is None: cur_dir = cur_pos - Vec2d(trajectory[i-1])
      else:                           cur_dir = last_dir
      
    return cur_pos, cur_dir, Vec2d(trajectory[i+1]) if i+1 < len(trajectory)  else None
  
  def get_formation_at(self, pos, angle):
    pos = np.array(list(pos))
    n_ranks, n = self.n_ranks, self.n
    size, dist = self.soldier_size_dist
    return self.formation.get_formation(pos, angle, n_ranks, n, size, dist)
  
  def set_formation(self, f1, ranks_ind, traj_form, is_LSA):
  
    if is_LSA:
      # print("CHANGE",i, dir_angle)
      ranks = [[] for _ in range(max(ranks_ind.values()) + 1)]
      
      pd = pairwise_distances(f1, traj_form[-1])
      row_ind, col_ind = linear_sum_assignment(pd)
      
      soldiers = []
      for i in range(len(row_ind)):
        d = Vec2d(f1[i].tolist())
        s = self.soldiers[col_ind[i]]
        ranks[ranks_ind[i]].append(s)
        coord = list((ranks_ind[i], len(ranks[ranks_ind[i]]) - 1))
        s.trajectory.append((d, coord))
        soldiers.append(s)
      self.soldiers = soldiers
  
    for i,s in enumerate(self.soldiers):
      d = Vec2d(f1[i].tolist())
      if len(s.trajectory) > 0: coord = s.trajectory[-1][1] 
      else: coord = s.coord
      s.trajectory.append((d, coord))
    
  def update_soldiers_trajectory(self):
    for s in self.soldiers:
      s.target_position, s.coord = s.trajectory[0]
      s.changed_target = False
    
    
  def set_spline_formation_trajectory(self, start_dir, final_dir, trajectory, show = False):
    spline_trajectory, spline_directions = self.get_spline(np.array(trajectory), show)
    
    last_dir = start_dir
    traj_form = [self.get_soldiers_pos(False)]
    for i in range(len(spline_trajectory)):
      
      cur_pos, cur_dir, next_pos = self.get_cur_traj(spline_trajectory, spline_directions, i, last_dir, final_dir)
      dir_angle = abs((last_dir).get_angle_degrees_between(cur_dir)) 
      formation_angle = (cur_dir).perpendicular().angle  
      
      if show: plt.scatter(*cur_pos, color = "blue" if self.check_dir_angle(dir_angle) < 50 else "red")
      
      f1, ranks_ind = self.get_formation_at(np.array(list(cur_pos)), formation_angle)
      self.set_formation(f1, ranks_ind, traj_form, self.check_dir_angle(dir_angle))
      
      last_dir = cur_dir
      traj_form.append(f1)
      
    self.update_soldiers_trajectory()
      
    self.traj = spline_trajectory
  

  def __init__(self, game, pos, angle, col, coll):

    self.col = col
    self.coll = coll
    self.game = game
    self._n = self.start_n
    self.n_ranks = self.start_ranks
    self.angle = angle
    self.final_pos = pos

    self.is_selected = False
    self.is_moving = False
    self.order = None
    self.before_order = None
    self.units_fighting_against = set()
    self.traj = []
    self.state = UnitState.STAND

    # Adding the soldiers physically
    self.add_soldiers()
    
    self.order, r_ind = self.formation.get_formation(
      pos, angle, self.n_ranks, self.n, *self.soldier_size_dist
    )
    self.formation.execute_formation(self.order, r_ind, set_physically=True)

  def add_soldiers(self):
    soldiers = []
    for _ in range(self.start_n):
      s = self.soldier(self.game, Vec2d(), self.col, self.coll)
      s.unit = self
      soldiers.append(s)
    self.soldiers = soldiers

  def update_soldiers(self):
    
    distances = []
    changed = []
    max_length_traj = 0
    update_soldiers = False
    for s in self.soldiers:
      if len(s.trajectory) > 0: update_soldiers = True
      distances.append(s.target_position.get_dist_sqrd(s.body.position)**0.5)
      if  distances[-1] < 12 and len(s.trajectory)>1: 
        s.next_target()
      changed.append(s.changed_target) 
      max_length_traj = max(len(s.trajectory), max_length_traj)
    
    if not update_soldiers: return
    
    sum_changed = sum(changed)
    if sum_changed == self.n:
      for s in self.soldiers: s.changed_target = False
      changed = [False] * self.n
    # elif (self.n - sum_changed)/self.n <= 0.1:
    #   for s,c in zip(self.soldiers, changed):
    #     if c: 
    #       s.next_target()
    #       s.changed_target = False
    #   changed = [False] * self.n
      
    
    changed_soldiers_distances = np.array(distances)[True != np.array(changed)]
    t = changed_soldiers_distances.mean() / s.base_speed
    min_dist = np.min(changed_soldiers_distances)
    for i,s in enumerate(self.soldiers):
      
      # if s.changed_target:
      #   cur_speed = s.base_speed / 4 # min_dist / t
      if len(s.trajectory) < max_length_traj:
        # cur_speed = s.base_speed / 10 # min_dist / t
        cur_speed =  min_dist / t * 0.1
      else:
        cur_speed = distances[i] / t
      if s.target_position.get_dist_sqrd(s.body.position) < 2: continue
      s.body.angle = (s.target_position - s.body.position).angle
      s.move(cur_speed, self.LATERAL_NOISE_MULTIPLIER)


  def update(self, dt):
    self.update_soldiers()
    

  def draw(self, DEBUG):
    for s in self.soldiers: s.draw(DEBUG)

    if DEBUG:
      # DRAW CONVEX HULL
      inf_simplices, fight_simplices = self.formation.get_hulls(for_draw=True)    
      for p1, p2 in inf_simplices:
        pygame.draw.line(
          self.game.screen, (0, 0, 0), (p1[0], p2[0]), (p1[1], p2[1])
        )
      for p1, p2 in fight_simplices:
        pygame.draw.line(
          self.game.screen, (0, 0, 0), (p1[0], p2[0]), (p1[1], p2[1])
        )
        
      
      # DRAW DIRECTION
      center, direction = self.pos_direction
      p1 = to_pygame(center, self.game.screen)
      p2 = to_pygame(center + direction*30, self.game.screen)
      pygame.draw.aalines(self.game.screen, GREEN, False, [p1,p2], 10)
        
        

    