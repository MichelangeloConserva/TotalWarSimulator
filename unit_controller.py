from unit_movement_controller import MovementController
from unit_fight_controller import FightController
from utils.enums import UnitState

FORMATION_UPDATE_EVERY = 30


class UnitController:
  def __init__(self, unit, formation):
    self.unit = unit
    self.movement_controller = MovementController(unit, formation)
    self.fight_controller = FightController(unit, self)
    
    self.steps = 0

  def move_at_point(self, final_pos, final_angle=None, n_ranks=None, remove_tu=True):

    if self.unit.state == UnitState.FIGHT:
      print("Order to move while fighting we must do something different")

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
    if self.steps > FORMATION_UPDATE_EVERY or n_deads > 0:
      self.update_formation()


    # Moving the soldier if not fighting
    if self.unit.state != UnitState.FIGHT:
      self.movement_controller.move_soldiers()
    # Fighting is different
    else:
      self.fight_controller.update()
    


  def update_formation(self):
    
    if self.unit.state != UnitState.FIGHT:
      self.movement_controller.update_formation()
    else:
      self.fight_controller.update_formation()

