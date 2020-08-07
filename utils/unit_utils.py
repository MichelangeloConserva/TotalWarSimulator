import numpy as np

from pymunk.vec2d import Vec2d

from .pymunk_utils import rotate_matrix, spaced_vector


def get_formation(pos, angle, n_ranks, n, size, dist):

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
