import numpy as np

from pymunk.vec2d import Vec2d

from .geom_utils import rotate_matrix, spaced_vector
# from geom_utils import rotate_matrix, spaced_vector

def get_formation(pos, angle, n_ranks, n, size, dist):
    formation = []
    rank_ind = []
    # TODO: check if this needs to be reversed
    k = 0
    hh = spaced_vector(n_ranks, size, dist)[::-1]
    for i in range(n_ranks):
        cur = []
        if i == n_ranks-1:  cur_n_rank = n - len(formation)
        else:               cur_n_rank = int(np.ceil(n / n_ranks))
        rank_positions = spaced_vector(cur_n_rank, size, dist)
        for w in rank_positions:
            formation.append((w, hh[i], 0))
            cur.append(k)
            k += 1
        rank_ind.append(cur.copy())
        cur = []
    
    return (pos + rotate_matrix(np.array(formation),angle)), rank_ind










