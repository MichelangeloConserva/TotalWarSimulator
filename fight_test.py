import numpy as np
import pygame
import pymunk

from pymunk.vec2d import Vec2d
from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_SPACE
from scipy import interpolate
from time import time

from inf_unit import Melee_Unit
from utils.colors import WHITE, BLUE, GREEN, RED
from utils.pymunk_utils import calc_vertices
from utils.settings.game import WIDTH, HEIGHT
from game import Game
from utils.enums import UnitState

from utils.pymunk_utils import do_polygons_intersect


record = False
DEBUG = False

class Trajectory:
  def __init__(self, screen, n=10, r=200):

    self.screen = screen

    self.step = 30
    self.width = 3
    self.n = n

    center = Vec2d(WIDTH // 2, HEIGHT // 2)
    self.control_vertex_list = calc_vertices(center, WIDTH // 3, HEIGHT // 3, np.pi)
    self.control_vertex_list = [list(v) for v in self.control_vertex_list]

  def make_curve_points(self, connected=False):
    x, y = zip(*self.control_vertex_list)

    # closing the trajectory
    # x = np.r_[x, x[0]]
    # y = np.r_[y, y[0]]

    f, u = interpolate.splprep([x, y], s=0, k=2)
    xint, yint = interpolate.splev(np.linspace(0, 1, 50), f)
    self.curve_vertex_list = [(x, y) for x, y in zip(xint, yint)]

  def draw(self):
    for i, vertex in enumerate(self.control_vertex_list):
      pygame.draw.circle(self.screen, BLUE, (int(vertex[0]), int(vertex[1])), 7)

    self.make_curve_points(True)
    pygame.draw.lines(self.screen, GREEN, False, self.curve_vertex_list, self.width)
    # Draw last line from last curve point to last control point




class UnitStateChecker:
  
  @property
  def all_units(self): return self.unit_defender + self.units_attacker
  
  def __init__(self, game):
    self.game = game
    
    self.units_attacker = []
    self.unit_defender = []
  
  def add_unit(self, is_attacker, *units):
    if is_attacker:
      for u in units: self.units_attacker.append(u)
    else: 
      for u in units: self.unit_defender.append(u)

  def update_states(self):
    
    for u in self.all_units:
       
      # Check if fighting
      for o in self.all_units:
        if o == u: continue
          # Checking if fighting hull intersects
    
        u_fight_hull = u.fight_hull
        o_fight_hull = o.fight_hull
    
        if u_fight_hull.intersects(o_fight_hull):
          u.state = o.state = UnitState.FIGHT
          
          u.units_fighting_against.add(o)
          o.units_fighting_against.add(u)

      # Checking movement
      if u.state == UnitState.MOVE and not u.is_moving: # The unit has just stopped
        u.state = UnitState.STAND   
      elif u.state == UnitState.STAND and u.is_moving:
        u.state = UnitState.MOVE





if __name__ == "__main__":
  game = Game(record)
  dt = 1 / game.fps

  k = 0
  traj = Trajectory(game.screen)

  inf = Melee_Unit(game, (300, 300), np.pi, RED, 1)
  inf2 = Melee_Unit(game, traj.control_vertex_list[1], np.pi, BLUE, 1)


  army1 = [inf]
  army2 = [inf2]

  usc = UnitStateChecker(game)
  usc.add_unit(True, inf)
  usc.add_unit(False, inf2)


  cur_drag_ind = None
  start = time()
  stop = False
  start = time()
  while not game.done:
    game.screen.fill(WHITE)

    for event in pygame.event.get():

      if event.type == QUIT or event.type == KEYDOWN and event.key == K_ESCAPE:
        pygame.quit()
        game.done = True
        if game.record:
          game.save_video()  # might take some time to run

      if event.type == KEYDOWN and event.key == K_SPACE:
        stop = not stop

      # Moving the points around
      if event.type == pygame.MOUSEBUTTONDOWN:
        if event.button == 1:
          pos = pygame.mouse.get_pos()

          for i, p in enumerate(traj.control_vertex_list):
            if (Vec2d(pos) - Vec2d(list(p))).length < 10:
              cur_drag_ind = i
              break

      if event.type == pygame.MOUSEBUTTONUP: cur_drag_ind = None

    if not cur_drag_ind is None: traj.control_vertex_list[i] = pygame.mouse.get_pos()

    if game.done: break


    inf.update_info()   # like hulls and position
    inf2.update_info()
    
    # Checking if units are fighting
    usc.update_states()


    if inf.state == UnitState.STAND:
      
      # Checking is we are going to attack another unit
      from shapely.geometry import Point
      p = inf2.inf_hull
      point = Point(traj.control_vertex_list[k])
      
      if p.contains(point):
        inf.controller.attack(inf2)
      else:
        inf.move_at_point(traj.control_vertex_list[k])
        k = (k + 1) % len(traj.control_vertex_list)


    # CHECKING COLLISION BETWEEN FIGHTING AREA OF THE UNITS
    # from utils.pymunk_utils import do_polygons_intersect


    if inf.state == UnitState.FIGHT:
      print("STOPPED")
      stop = True


    # if time() - start > 2:
    #   start = time()
    #   np.random.choice(inf.soldiers).health = -1

    if not stop:
      inf.update(dt)
      inf2.update(dt)
      game.update(dt)

    game.draw(DEBUG)
    inf.draw(DEBUG)
    inf2.draw(DEBUG)
    traj.draw()

    pygame.display.flip()
    game.clock.tick(game.fps)

    if game.record:
      arr = pygame.surfarray.array3d(game.screen).swapaxes(1, 0)
      game.video.append(arr.astype(np.uint8))


# import matplotlib.pyplot as plt

# plt.scatter(*np.array(inf.get_soldiers_pos(True)).T)
# plt.scatter(*np.array(vertices).T)

# plt.scatter(*np.array(inf2.get_soldiers_pos(True)).T)
# plt.scatter(*np.array(vertices2).T)



