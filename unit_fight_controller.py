import numpy as np

from pymunk.vec2d import Vec2d
from utils.enums import UnitState

from utils.pymunk_utils import do_polygons_intersect,polygon_min_dist

class FightController:
  
  def __init__(self, unit, controller):
    self.unit = unit
    self.controller = controller
    
    self.target = None
  
  
  def update_formation(self):
    
    if not self.target is None:
      
      # Check if we are in fighting range
      u_fight_hull = self.unit.fight_hull
      t_fight_hull = self.target.fight_hull
      
      dist = u_fight_hull.distance(t_fight_hull)
      if dist > 0:
        # Then we are charging
        
        # We want to face the enemy
        unit_pos = self.unit.pos
        other_pos = self.target.pos
        final_angle = Vec2d(list(unit_pos-other_pos)).perpendicular().angle
        self.controller.movement_controller.move_at_point(
          other_pos, final_angle=final_angle)        
        
  
  def update(self):

      for s in self.unit.soldiers:
        if len(s.enemy_in_range) != 0: return

      if not self.target is None:
        unit_pos = self.unit.pos
        other_pos = self.target.pos
        final_angle = Vec2d(list(unit_pos-other_pos)).perpendicular().angle
        self.controller.movement_controller.move_at_point(
          other_pos, final_angle=final_angle)            





