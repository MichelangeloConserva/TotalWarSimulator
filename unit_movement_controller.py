import sys, os  # Dirty trick to allow sibling imports
sys.path.insert(0, os.path.abspath(".."))

import numpy as np
import pygame

from pymunk.vec2d import Vec2d
from pymunk.pygame_util import to_pygame, from_pygame

speed_th_distance = 3
destination_weight = 0.1

class MovementController:
  
  def __init__(self, unit, formation):
    self.unit = unit
    self.formation = formation
    self.steps = 0

  def move_at_point(self, final_pos, final_angle=None, n_ranks=None, remove_tu=True):
    
    if type(final_pos)==list:    final_pos = np.array(final_pos)
    elif type(final_pos)==Vec2d: final_pos = np.array(list(final_pos))
    
    start_pos = self.unit.pos
    diff = start_pos - final_pos

    # Setting angle and n_ranks to default values if not provided
    if final_angle is None:
      final_angle = diff.perpendicular().angle
    if n_ranks is None:
      self.formation.update_n_ranks()
      n_ranks = self.unit.n_ranks
    
    self.unit.final_pos = final_pos
    self.unit.angle = final_angle    
    
    final_pos = final_pos*destination_weight + start_pos*(1-destination_weight)
    
    size, dist = self.unit.soldier_size_dist
    dest_formation, ranks_ind = self.formation.get_formation(
      final_pos, final_angle, n_ranks, self.unit.n, size, dist
    )

    # The formation at the destination
    self.formation.execute_formation(dest_formation, ranks_ind)
    self.unit.ranks_ind = ranks_ind

  def update_formation(self):
    self.move_at_point(self.unit.final_pos, self.unit.angle)
    self.steps = 0

  def move_soldiers(self):
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
      elif max_ds - d > speed_th_distance:
        speed = s.base_speed * 0.1
      else:
        speed = s.base_speed

      s.body.angle = (s.target_position - s.body.position).angle
      s.move(
        speed, self.unit.FORCE_MULTIPLIER, self.unit.LATERAL_NOISE_MULTIPLIER,
      )

    # If one soldier is moving the the unit is moving
    if self.unit.is_moving != is_moving:
      self.unit.is_moving = is_moving
