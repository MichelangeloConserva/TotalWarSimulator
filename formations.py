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
from shapely.geometry import Point
from shapely.geometry.polygon import Polygon
      
from utils.pymunk_utils import rotate_matrix, spaced_vector


class BaseFormation:
  def __init__(self, unit, melee_range):
    self.melee_range = melee_range
    self.unit = unit
    self.space = unit.game.space
    self.intra_springs = set()

  def get_hulls(self, for_draw = False):
    points = np.array(self.unit.get_soldiers_pos(False))
    if for_draw:
      for i in range(len(points)):
        points[i] = to_pygame(list(points[i]), self.unit.game.screen)
    center = points.mean(0)
    inf_hull_ = ConvexHull(points)
    inf_hull = Polygon(points[inf_hull_.vertices])

    expanded = []
    for ind1, ind2 in zip(
      inf_hull_.vertices, inf_hull_.vertices[1:].tolist() + [inf_hull_.vertices[0]]
    ):
      v1 = Vec2d(list(points[ind1]))
      v2 = Vec2d(list(points[ind2]))

      p_norm = (v1 - v2).perpendicular_normal() * self.melee_range
      if (v1 + p_norm - center).length < (v1 - center).length:
        p_norm = -p_norm
      v1 = v1 + p_norm
      v2 = v2 + p_norm

      expanded.append(v1)
      expanded.append(v2)

    expanded = np.array([list(p) for p in expanded])

    hull = ConvexHull(expanded)
    fight_hull = Polygon(expanded[hull.vertices])

    if not for_draw:  # we only need polygons if we are not drawing
      return inf_hull, fight_hull

    
    inf_simplices = []
    for simplex in inf_hull_.simplices:
      inf_simplices.append(
        (points[simplex, 0].tolist(), points[simplex, 1].tolist())
      )
    fight_simplices = []
    for simplex in hull.simplices:
      fight_simplices.append(
        (expanded[simplex, 0].tolist(), expanded[simplex, 1].tolist())
      )
    return inf_simplices, fight_simplices


  def get_formation(self, *args, **kwargs):
    raise NotImplementedError("get_formation")

  def execute_formation(self, *args, **kwargs):
    raise NotImplementedError("execute_formation")


class SquareFormation(BaseFormation):
  def __init__(self, unit, melee_range, ratio):
    BaseFormation.__init__(self, unit, melee_range)
    self.ratio = ratio

  def update_n_ranks(self):
    """
  When soldiers die we want to maintain the same ratio of the unit between 
  ranks and columns.
  """
    self.unit.n_ranks = math.ceil((self.unit.n / self.ratio) ** 0.5)

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

    pd = pairwise_distances(formation, self.unit.get_soldiers_pos())**2
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

  def formation_info_update(self):
    self.unit.inf_hull, self.unit.fight_hull = self.get_hulls()
    self.unit.pos = self.unit._pos


## OLD CODE FOR CONVEX HULL ##
# points = np.array(self.unit.get_soldiers_pos(False))
# center = points.mean(0)
# for i in range(len(points)):
#   diff = Vec2d(list(points[i] - center))
#   l = diff.normalize_return_length()
#   # Expanding the convex hull according the the melee range
#   # TODO : the expansion can be done in a better way
#   # using the center is lame
#   p = list( diff*(l+self.melee_range) + Vec2d(list(center)))
#   points[i] = to_pygame(p, self.unit.game.screen) if in_pygame else p
# hull = ConvexHull(points)
# vertices = []
# simplices = []
# for simplex in hull.simplices:
#   # vertices.append(points[simplex,0].tolist())
#   vertices.append((points[simplex,0][0],points[simplex,1][0]))
#   vertices.append((points[simplex,0][1],points[simplex,1][1]))
#   simplices.append((points[simplex,0].tolist(),points[simplex,1].tolist()))
# return vertices, simplices
