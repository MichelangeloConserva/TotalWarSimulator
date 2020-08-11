import sys, os  # Dirty trick to allow sibling imports

sys.path.insert(0, os.path.abspath(".."))

import numpy as np
import pygame

from pymunk.vec2d import Vec2d
from pymunk.pygame_util import to_pygame, from_pygame


class MovementController:
  
  def __init__(self, unit, formation):
    self.unit = unit
    self.formation = formation
    self.steps = 0

  def move_at_point(self, final_pos, final_angle=None, n_ranks=None, remove_tu=True):
    start_pos = self.unit.pos

    # The vector representing the front line
    diff = start_pos - final_pos

    # TODO : check if the n_ranks has to be changed due to deaths
    # Setting angle and n_ranks to default values if not provided
    if final_angle is None:
      final_angle = diff.perpendicular().angle
    if n_ranks is None:
      self.formation.update_n_ranks()
      n_ranks = self.unit.n_ranks

    size, dist = self.unit.soldier_size_dist

    dest_formation, ranks_ind = self.formation.get_formation(
      np.array(list(final_pos)), final_angle, n_ranks, self.unit.n, size, dist
    )

    # The formation at the destination
    self.unit.order, self.unit.ranks_ind = dest_formation, ranks_ind
    self.formation.execute_formation(self.unit.order, self.unit.ranks_ind)
    
    self.unit.final_pos = final_pos
    self.unit.angle = final_angle


  def update(self, n_deads):
    self.steps += 1
    
    if self.steps > 60 or n_deads > 0:
      self.move_at_point(self.unit.final_pos, self.unit.angle)
      self.formation.execute_formation(self.unit.order, self.unit.ranks_ind)
      self.steps = 0
    
    ds = [(s.body.position - s.target_position).length for s in self.unit.soldiers]
    max_ds = max(ds)

    # Moving the soldiers
    is_moving = False
    for i, s in enumerate(self.unit.soldiers):
      d = ds[i]

      if d < s.size / 6:
        s.body.velocity = 0, 0
        s.body.angular_velocity = 0.0
        continue
      if not is_moving:
        is_moving = True

      if len(s.enemy_melee_range):
        speed = s.base_speed * self.unit.ATTACKING_MULTIPLIERS
      elif max_ds - d > 5:
        speed = s.base_speed * 0.1
      else:
        speed = s.base_speed

      s.body.angle = (s.target_position - s.body.position).angle
      s.move(
        speed,
        self.unit.FORCE_MULTIPLIER,
        self.unit.LATERAL_NOISE_MULTIPLIER,
      )

    # If one soldier is moving the the unit is moving
    if self.unit.is_moving != is_moving:
      self.unit.is_moving = is_moving
      



# import math
# n = 100
# ratio = 4

# for n in range(1,101)[::-1]:
  
#   p,_=self.get_formation(
#       np.array((0,0)), 0, ), n, size, dist
#     )
  
#   plt.scatter(*p.T)
#   plt.title(f"{n}, {math.ceil((n / ratio)**0.5)}")
#   plt.show()
