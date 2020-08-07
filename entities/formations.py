import sys, os  # Dirty trick to allow sibling imports

sys.path.insert(0, os.path.abspath(".."))

import numpy as np
import pymunk

from pymunk.vec2d import Vec2d
from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment

from utils.pymunk_utils import rotate_matrix, spaced_vector


class BaseFormation:
  def __init__(self, unit):

    self.unit = unit
    self.space = unit.game.space
    self.intra_springs = set()

  def remove_springs(self):
    for s in self.intra_springs:
      if s in self.space._constraints:
        self.space.remove(s)
    self.intra_springs = set()

  def right_nn_spring(self, s, n_r):

    # Linking neighbours
    s.right_nn = n_r
    n_r.left_nn = s

    holder = n_r.holder
    joint = pymunk.PinJoint(holder, s.holder)
    joint.distance = self.unit.rest_length_intra
    joint.collide_bodies = False
    self.space.add(joint)
    self.intra_springs.add(joint)

    if not n_r.front_nn is None:
      holder = n_r.front_nn.holder
      joint = pymunk.PinJoint(holder, s.holder)
      joint.distance = (
        self.unit.rest_length_intra ** 2 + self.unit.rest_length_intra ** 2
      ) ** 0.5
      joint.collide_bodies = False
      self.space.add(joint)
      self.intra_springs.add(joint)

    if not s.front_nn is None:
      holder = s.front_nn.holder
      joint = pymunk.PinJoint(holder, n_r.holder)
      joint.distance = (
        self.unit.rest_length_intra ** 2 + self.unit.rest_length_intra ** 2
      ) ** 0.5
      joint.collide_bodies = False
      self.space.add(joint)
      self.intra_springs.add(joint)

  def bot_nn_spring(self, s, n_r):
    # Linking neighbours
    s.bot_nn = n_r
    n_r.front_nn = s

    holder = n_r.holder
    joint = pymunk.PinJoint(holder, s.holder)
    joint.distance = self.unit.rest_length_intra
    joint.collide_bodies = False
    self.space.add(joint)
    self.intra_springs.add(joint)

  def add_intra_springs_and_set_soldier_dest(self):

    # Removing intra_springs while changing formation
    self.remove_springs()

    # Resetting the neighbours of each soldier
    for s in self.unit.soldiers:
      s.reset_nn()

    ranks = self.ranks

    k = 0
    for i in range(len(ranks) - 1):
      for j in range(len(ranks[i])):
        s = ranks[i][j]
        s.target_position = Vec2d(self.unit.order[k].tolist())
        k += 1

        if j + 1 < len(ranks[i]):
          self.right_nn_spring(s, ranks[i][j + 1])
        if i + 1 < len(ranks) and i != len(ranks) - 2:  # (not last rank)
          self.bot_nn_spring(s, ranks[i + 1][j])

    # The last rank must be treated differently since it may have less soldiers
    l_rank = ranks[-1]
    for j in range(len(l_rank)):
      s = ranks[-1][j]
      s.target_position = Vec2d(self.unit.order[k].tolist())
      k += 1

      if j + 1 < len(l_rank):
        self.right_nn_spring(s, l_rank[j + 1])

      # We must find the front neighbour manually
      front_nn = np.argmin(
        [(s.body.position - o.body.position).length for o in ranks[-2]]
      )
      self.bot_nn_spring(ranks[-2][front_nn], s)  # N.B. order inverted

  def check_for_deads(self):
    changed = False
    for s in self.unit.soldiers:
      if not s.is_alive:

        s.dies()
        self.unit.soldiers.remove(s)

        self.substitute_dead(s, s.holder, s.coord, *s.nns())
        changed = True

    return changed

  def get_formation(self, *args, **kwargs):
    raise NotImplementedError("get_formation")

  def execute_formation(self, *args, **kwargs):
    raise NotImplementedError("execute_formation")

  def substitute_dead(self, *args, **kwargs):
    raise NotImplementedError("substitute_dead")


class SquareFormation(BaseFormation):
  def __init__(self, unit):
    BaseFormation.__init__(self, unit)

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

    pd = pairwise_distances(formation, self.unit.soldiers_pos)
    row_ind, col_ind = linear_sum_assignment(pd)
    for i in range(len(row_ind)):
      d = Vec2d(formation[i].tolist())
      s = self.unit.soldiers[col_ind[i]]

      if set_physically:
        s.body.position = d
        s.holder.position = d

      # Each soldiers knows its destination and is in a particular rank
      s.target_position = d
      self.ranks[ranks_ind[i]].append(s)
      s.coord = list((ranks_ind[i], len(self.ranks[ranks_ind[i]]) - 1))

  def substitute_dead(self, s, holder, coord, front_nn, right_nn, bot_nn, left_nn):

    if bot_nn is None:
      if not s.left_nn is None:
        s.left_nn.right_nn = s.right_nn
      else:
        print("THIS NOW")
      if not s.right_nn is None:
        s.right_nn.left_nn = s.left_nn
      else:
        print("THIS NOW")

    else:
      while not bot_nn is None:

        # Updating links
        self.ranks[coord[0]][coord[1]] = bot_nn
        bot_nn.coord, coord = coord, bot_nn.coord

        old_r_nn, old_l_nn = bot_nn.right_nn, bot_nn.left_nn
        bot_nn.right_nn, bot_nn.left_nn = left_nn, right_nn
        bot_nn.holder, holder = holder, bot_nn.holder
        self.game.space.remove(bot_nn.spring)
        bot_nn.attach_to_holder()

        bot_nn = bot_nn.bot_nn

        if not old_r_nn is None:
          old_r_nn.left_nn = None
        if not old_l_nn is None:
          old_l_nn.right_nn = None

        self.ranks[coord[0]].remove(self.ranks[coord[0]][coord[1]])

        for s in self.ranks[coord[0]]:
          if s.coord[1] > coord[1]:
            s.coord[1] -= 1

      self.game.space.remove(holder.constraints)
      self.game.space.remove(holder.shapes)
      self.game.space.remove(holder)
