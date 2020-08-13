from unit_movement_controller import MovementController
from unit_fight_controller import FightController
from utils.enums import UnitStatus

class UnitController:
  def __init__(self, unit, formation):
    self.unit = unit
    self.movement_controller = MovementController(unit, formation)
    self.fight_controller = FightController(unit, self)
    
    self.steps = 0
    self.status = UnitStatus.STAND

  def move_at_point(self, final_pos, final_angle=None, n_ranks=None, remove_tu=True):

    if self.status == UnitStatus.FIGHT:
      print("Order to move while fighting we must do something different")

    self.status = UnitStatus.MOVE
    self.movement_controller.move_at_point(
      final_pos, final_angle=None, n_ranks=None, remove_tu=True
    )
    
  def attack(self, other_unit):
    self.fight_controller.target = other_unit
    self.fight_controller.update_formation()

  def update(self, n_deads):

    # We want to update the formation every once in a while or after a soldier
    # dies so that everything is perfectly in order    
    self.steps += 1
    if self.steps > 60 or n_deads > 0:
      self.update_formation()


    # Moving the soldier if not fighting
    if self.status != UnitStatus.FIGHT:
      self.movement_controller.move_soldiers()
    # Fighting is different
    else:
      self.fight_controller.update()
    
    # Checking the status
    if self.status == UnitStatus.MOVE and not self.unit.is_moving:
      self.status = UnitStatus.STAND

  def update_formation(self):
    
    if self.status != UnitStatus.FIGHT:
      self.movement_controller.update_formation()
    else:
      self.fight_controller.update_formation()

