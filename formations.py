import sys, os  # Dirty trick to allow sibling imports

sys.path.insert(0, os.path.abspath(".."))

import numpy as np
import pymunk, pygame
import math

from pymunk.vec2d import Vec2d
from pymunk.pygame_util import to_pygame
from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment
from scipy.spatial import ConvexHull

from utils.pymunk_utils import rotate_matrix, spaced_vector


class BaseFormation:
  
  def __init__(self, unit, melee_range):
    self.melee_range = melee_range
    self.unit = unit
    self.space = unit.game.space
    self.intra_springs = set()
  
  def get_melee_fighting_hull(self, in_pygame = False):
    points = np.array(self.unit.get_soldiers_pos(False))
    center = points.mean(0)
    for i in range(len(points)):
      diff = Vec2d(list(points[i] - center))
      l = diff.normalize_return_length()
      # Expanding the convex hull according the the melee range
      # TODO : the expansion can be done in a better way
      # using the center is lame
      p = list( diff*(l+self.melee_range) + Vec2d(list(center)))
      points[i] = to_pygame(p, self.unit.game.screen) if in_pygame else p
    return points, ConvexHull(points)
  
  def get_formation(self, *args, **kwargs):
    raise NotImplementedError("get_formation")

  def execute_formation(self, *args, **kwargs):
    raise NotImplementedError("execute_formation")

  def substitute_dead(self, *args, **kwargs):
    raise NotImplementedError("substitute_dead")



class SquareFormation(BaseFormation):
  def __init__(self, unit, melee_range, ratio):
    BaseFormation.__init__(self, unit, melee_range)
    self.ratio = ratio

  def update_n_ranks(self):
    self.unit.n_ranks = math.ceil((self.unit.n / self.ratio)**0.5)



  def get_formation(self, pos, angle, n_ranks, n, size, dist):

    formation = []
    rank_ind = {}

    k = 0
    hh = spaced_vector(n_ranks, size, dist)[::-1]
    for i in range(n_ranks):
      if i == n_ranks - 1:
        cur_n_rank = n - len(formation)
      else:
        cur_n_rank = int(np.ceil(n / n_ranks))
      rank_positions = spaced_vector(cur_n_rank, size, dist)
      for w in rank_positions:
        formation.append((w, hh[i], 0))
        rank_ind[k] = i
        k += 1

    return (pos + rotate_matrix(np.array(formation), angle)), rank_ind

  def execute_formation(self, formation, ranks_ind, set_physically=False):

    # Storing the rank information
    self.ranks = [[] for _ in range(max(ranks_ind.values()) + 1)]

    pd = pairwise_distances(formation, self.unit.get_soldiers_pos())
    
    # if pd.max() > sum(self.unit.soldier_size_dist):
    #   pd[pd==0] = 100
    
    row_ind, col_ind = linear_sum_assignment(pd)
    
    for i in range(len(row_ind)):
      d = Vec2d(formation[i].tolist())
      s = self.unit.soldiers[col_ind[i]]

      # Each soldiers knows its destination and is in a particular rank
      s.target_position = d
      self.ranks[ranks_ind[i]].append(s)
      s.coord = list((ranks_ind[i], len(self.ranks[ranks_ind[i]]) - 1))
      
      if set_physically:
        s.body.position = d



# import matplotlib.pyplot as plt

# s_pos = np.array(self.unit.get_soldiers_pos(False))


# plt.scatter(*s_pos.T)
# plt.scatter(*formation.T)

# pd = pairwise_distances(formation, self.unit.get_soldiers_pos())
# row_ind, col_ind = linear_sum_assignment(pd)

# for i in range(len(row_ind)):
#   d = Vec2d(formation[i].tolist())
#   s = self.unit.soldiers[col_ind[i]].body.position
#   plt.plot(*np.vstack((formation[i], list(s))).T)
