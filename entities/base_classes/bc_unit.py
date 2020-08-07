import numpy as np
import pygame
import pymunk

from pymunk.vec2d import Vec2d
from pymunk.pygame_util import to_pygame, from_pygame

from utils.pygame_utils import draw_text

class Unit:

  @property
  def soldier_size_dist(self): return self.soldier.radius*2, self.soldier.dist
  @property
  def max_width(self): return self.calc_width(self.n//3)
  @property
  def min_width(self): return self.calc_width(self.n//10)
  @property
  def width(self):     return self.calc_width(len(self.ranks[0]))
  @property
  def pos(self): return sum(self.soldiers_pos) / len(self.soldiers)
  @property
  def soldiers_pos(self): return [s.body.position for s in self.soldiers]
  @property
  def n(self): return len(self.soldiers)
  @property
  def border_units(self):
    return [s for s in self.soldiers if s.is_at_border]
  @property
  def target_unit(self): return self._target_unit
  @target_unit.setter
  def target_unit(self, tu):
    self._target_unit = tu
    if tu is None: return
    self.move_at_point(tu.pos, remove_tu=False) # Facing the enemy unit
  @property
  def is_moving(self): return self._is_moving
  @is_moving.setter
  def is_moving(self, v):
    """
    We set a timer from the moment the unit starts moving as this may be useful.
    """
    if not v: self.start_mov_time = pygame.time.get_ticks()
    self._is_moving = v

  def calc_width(self, upr):
    return upr*self.soldier.radius*2 + (upr-1) * self.soldier.dist


  def __init__(self, game, pos, angle, col, coll):

    self.col     = col
    self.coll    = coll
    self.game    = game
    self._n      = self.start_n
    self.n_ranks = self.start_ranks
    self.angle   = angle

    self.is_selected   = False
    self._is_moving    = False
    self._is_fighting  = False
    self._target_unit  = None
    self.order         = None
    self.before_order  = None

    # Adding the soldiers physically
    self.add_soldiers()

    # Instantiate them in the space
    self.order, r_ind = self.formation.get_formation(pos, angle, self.n_ranks,
               self.n, *self.soldier_size_dist)
    self.formation.execute_formation(self.order, r_ind, set_physically = True)

  def add_soldiers(self):
    soldiers = []
    for _ in range(self.start_n):
      s = self.soldier(self.game, Vec2d(), self.col, self.coll)
      s.unit = self
      soldiers.append(s)
    self.soldiers = soldiers

  def move_at(self, formation, formation_first, ranks_ind, remove_tu = True):
    """
    The function that actually execute the formation and set the order.
    This is low level since it requires the formations in input and doesn't
    crete them.'

    Parameters
    ----------
    formation : TYPE
      DESCRIPTION.
    formation_first : TYPE
      DESCRIPTION.
    ranks_ind : TYPE
      DESCRIPTION.
    remove_tu : TYPE, optional
      DESCRIPTION. The default is True.

    Returns
    -------
    None.

    """

    # self.before_order = self.soldiers_pos.copy() # if we need to copy the position before the order

    # Removing intra_springs while changing formation
    self.formation.remove_springs()

    # Remove target unit if changing pos
    if remove_tu: self.target_unit = None

    # Changing formation before moving
    self.formation.execute_formation(formation_first, ranks_ind)

    # The formation at the destination
    self.order = formation


  def move_at_point(self, final_pos, final_angle = None, n_ranks = None, remove_tu = True):
    """
    Given a position then the rotate formation towards the point and the final
    formation at the point are created and the soldiers are set the target
    destinations.

    Parameters
    ----------
    final_pos : TYPE
      DESCRIPTION.
    final_angle : TYPE, optional
      DESCRIPTION. The default is None.
    n_ranks : TYPE, optional
      DESCRIPTION. The default is None.
    remove_tu : TYPE, optional
      DESCRIPTION. The default is True.

    Returns
    -------
    None.

    """
    start_pos = self.pos

    # Checking is we need to start the first position a little bir further
    # this happens while fighting because the units cannot properly move while
    # fighting
    if self.is_fighting:
      # TODO : change these values to something meaningful
      start_pos = start_pos * 0.2 + final_pos * 0.8
    angle = self.angle

    # The vector representing the front line
    diff = (start_pos-final_pos)
    diff_p = diff.perpendicular()

    # Setting angle and n_ranks to default values if not provided
    if final_angle is None: final_angle = diff.perpendicular().angle
    if n_ranks     is None: n_ranks     = self.n_ranks

    # Checking if the unit formation must be rotated
    looking_dir = Vec2d(1,0).rotated(final_angle)
    invert = looking_dir.dot(diff_p) < 0
    if invert: angle = (-diff_p).angle
    else:      angle = diff_p.angle

    size, dist = self.soldier_size_dist
    # The unit is changing formation before starting to go tho the final destination
    here_formation, _ = self.formation.get_formation(np.array(list(start_pos)),
                                                     angle, n_ranks, self.n, size, dist)
    # The formation we actually want to go to
    dest_formation, ranks_ind = self.formation.get_formation(np.array(list(final_pos)),
                                                            final_angle, n_ranks, self.n, size, dist)
    self.move_at(dest_formation, here_formation, ranks_ind, remove_tu = remove_tu)

    self.angle = final_angle


  def update(self, dt):

    # Checking for changes in the formation due to deaths
    changed = self.formation.check_for_deads()

    ds = []
    if not self.order is None:

      # When we reach the new formation then add intra_springs
      # TODO : add a better criterion for creating intra_springs
      # this is not very good when in fighting
      ds = [(s.body.position-s.target_position).length for s in self.soldiers]
      if max(ds) < sum(self.soldier_size_dist):
        self.formation.add_intra_springs_and_set_soldier_dest()
        self.order = None


    # if not reordering and with a target unit then attack
    if self.order is None and not self.target_unit is None:
      self.attack(changed)

    # Checking if the unit is fighting
    is_fighting = self.is_fighting

    # Moving the soldiers
    is_moving = False
    for i,s in enumerate(self.soldiers):

      if s.target_position is None: continue
      if len(ds) == 0: d = (s.body.position-s.target_position).length
      else: d = ds[i]

      # Prevent tremors when reached position
      if d < s.size/10:
        s.body.velocity = 0,0
        s.body.angular_velocity = 0.
        continue
      if not is_moving: is_moving = True

      if len(s.enemy_melee_range): speed = s.base_speed * self.ATTACKING_MULTIPLIERS
      else: speed = s.base_speed

      s.body.angle = (s.target_position - s.body.position).angle
      s.move(speed, is_fighting, self.FORCE_MULTIPLIER, self.ATTACKING_MULTIPLIERS, self.LATERAL_NOISE_MULTIPLIER)

    # If one soldier is moving the the unit is moving
    if self.is_moving != is_moving: self.is_moving = is_moving

    # During the update the combact happens
    for s in self.soldiers: s.update(dt)


  def draw(self, DEBUG):
    for s in self.soldiers: s.draw()

    # if not self.target_unit is None:

    #     for s in self.soldiers:

    #         if s.target_soldier is None: continue

    #         p1 = to_pygame(s.body.position, self.game.screen)
    #         p2 = to_pygame(s.target_soldier.body.position, self.game.screen)

    #         pygame.draw.aalines(self.game.screen, RED, False, [p1,p2])


    ### ENEMY IN RANGE ###
    # for s in self.soldiers:
    #     draw_text(str(len(s.enemy_melee_range)), self.game.screen, self.game.font, s.body.position, 0)


    ## HEALTH ###
    # for s in self.soldiers:
    #     draw_text(str(round(s.health,1)), self.game.screen, self.game.font, s.body.position, 0)

    ### RANKS ###
    # for i,r in enumerate(self.ranks):
    #     for s in r:
    #         draw_text(str(i), self.game.screen, self.game.font, s.body.position, 0)

    ### COLS ###
    # for i,r in enumerate(self.ranks):
    #     for j,s in enumerate(r):
    #         if not s.is_alive or np.isnan(s.body.position[0]): continue
    #         draw_text(str(j), self.game.screen, self.game.font, s.body.position, 0)



    # if not self.before_order is None:
    #     for i,s in enumerate(self.soldiers):
    #         draw_text(str(i), self.game.screen, self.game.font, self.before_order[i], 0)
    #         draw_text(str(i), self.game.screen, self.game.font, s.body.position, 0)



    # for s in self.ranks[-1]:
    #     draw_text(str(1), self.game.screen, self.game.font, s.body.position, 0)
    #     pos = s.front_nn.body.position
    #     draw_text("f", self.game.screen, self.game.font, pos, 0)


    # for s in self.border_units:
    #     draw_text(str(1), sel

  def attack(self, *args, **kwargs):  raise NotImplementedError("attack")