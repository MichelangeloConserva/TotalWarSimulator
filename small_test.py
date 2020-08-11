import numpy as np
import pygame
import pymunk

from pymunk.vec2d import Vec2d
from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_SPACE
from scipy import interpolate
from time import time

from inf_sold import Melee_Soldier
from utils.colors import WHITE, BLUE, GREEN, RED
from utils.pymunk_utils import calc_vertices
from game import Game

record = False
DEBUG = True


class Trajectory:
  def __init__(self, screen, n=10, r=200):

    self.screen = screen

    self.step = 30
    self.width = 3
    self.n = n

    center = Vec2d(1500 // 2, 800 // 2)
    self.control_vertex_list = calc_vertices(center, 1500 // 4, 800 // 4, np.pi)
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


if __name__ == "__main__":
  game = Game(record)
  dt = 1 / game.fps

  k = 0
  traj = Trajectory(game.screen)

  s = Melee_Soldier(game, traj.control_vertex_list[0], BLUE, 1)

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

      if event.type == pygame.MOUSEBUTTONUP:
        cur_drag_ind = None

    if not cur_drag_ind is None:
      traj.control_vertex_list[i] = pygame.mouse.get_pos()

    if game.done:
      break

    # get_formation(pos, angle, self.n_ranks, self.n, size, dist)

    if not s.body.velocity.length > 1:
      s.target_position = traj.control_vertex_list[k]
      k = (k + 1) % len(traj.control_vertex_list)

    if not stop:

      s.body.angle = (s.target_position - s.body.position).angle
      s.move(30, 1, 1)

      s.update(dt)
      game.update(dt)

    game.draw(DEBUG)
    s.draw()
    traj.draw()

    pygame.display.flip()
    game.clock.tick(game.fps)

    if game.record:
      arr = pygame.surfarray.array3d(game.screen).swapaxes(1, 0)
      game.video.append(arr.astype(np.uint8))


# from scipy.spatial import ConvexHull, convex_hull_plot_2d
# import matplotlib.pyplot as plt

# points = np.array(s.soldiers_pos)
# hull = ConvexHull(points)

# plt.plot(points[:,0], points[:,1], 'o')
# for simplex in hull.simplices:
#     plt.plot(points[simplex, 0], points[simplex, 1], 'k-')
