import numpy as np

from pymunk.vec2d import Vec2d

from utils.pymunk_utils import do_polygons_intersect,polygon_min_dist

class FightController:
  
  def __init__(self, unit, controller):
    self.unit = unit
    self.controller = controller
    
    self.target = None
  
  
  def update_formation(self):
    
    
    # The target is the main focus of the unit
    if not self.target is None:
      
      # Check if we are in fighting range
      unit_vertices = self.unit.fight_hull_vertices
      other_vertices = self.target.fight_hull_vertices
      
      dist = polygon_min_dist(unit_vertices, other_vertices)
      if dist > 0:
        # Then we are charging
        
        # We want to face the enemy
        unit_pos = np.array(self.unit.get_soldiers_pos(False)).mean(0)
        other_pos = other_vertices.mean(0)
        inter_pos = other_pos*0.1 + unit_pos*0.9
        final_angle = Vec2d(list(other_pos-unit_pos)).perpendicular().angle
        self.controller.movement_controller.move_at_point(
          inter_pos, final_angle=final_angle)        
        
      else:
        # We are actually fighting
        pass  
  
  
  
  
  def update(self):

    if not self.target is None:
      pass
    else:
      pass
        

# plt.scatter(*unit_vertices.T)
# plt.scatter(*unit_pos)

# plt.scatter(*other_vertices.T)
# plt.scatter(*other_pos)





