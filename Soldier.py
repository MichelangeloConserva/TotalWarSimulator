import pymunk
import pygame
import numpy as np

from pymunk.pygame_util import to_pygame
from pymunk.vec2d import Vec2d

from utils.pymunk_utils import limit_velocity
from utils.pymunk_utils import kill_lateral_velocity
from utils.colors import BLACK, BLUE
  
class Soldier:

  friction = 1.1
  elasticity = 0.01
  mass = 5

  # BIG VERSION
  # radius = 7
  # dist = 5

  # MEDIUM VERSION
  radius = 5
  dist = 3

  # SMALL VERSION
  # radius = 2
  # dist =   1

  melee_range = 1
  # max_speed = 90
  # base_speed = 50
  max_speed   = 150
  base_speed  = 120

  max_health = 50
  attack = 10
  defense = 3

  # Holder spring setting
  stiffness = 5 * 15 * mass
  damping = 1

  @property
  def is_alive(self):   return self.health > 0
  @property
  def components(self): return [self.body, self.shape, self.sensor]
  
  def __init__(self, game, pos, col, coll):
    # Linking
    self.target_position = pos  # The location the soldier will go to
    self.col = col
    self.game = game

    # Physics
    self.body = self.add_body(pos)
    self.body.velocity_func = limit_velocity
    self.shape = self.add_shape(coll)
    self.sensor = self.add_sensors(coll)  # Melee sensor
    game.space.add(self.body, self.shape, self.sensor)

    # Variables
    self.enemy_melee_range = set()
    self.enemy_in_range = set()
    self.target_soldier = None  # TODO : To be used when fighting (?)
    
    self.health = self.max_health
    self.size = 2 * self.radius + self.dist

  def add_body(self, pos):
    body = pymunk.Body()
    body.position = pos
    body.soldier = self
    return body

  def move(self, speed, LATERAL_NOISE_MULTIPLIER):
    kill_lateral_velocity(self.body)
    f = speed * self.mass
    noise = np.random.randn() * self.mass * LATERAL_NOISE_MULTIPLIER
    self.body.apply_force_at_local_point(Vec2d(f, noise), Vec2d(0, 0))
    
  def dies(self):
    self.unit.soldiers.remove(self)
    for c in self.components: self.game.space.remove(c)

  def add_shape(self, coll):
    shape = pymunk.Circle(self.body, self.radius-1)
    shape.color = self.col
    shape.friction = self.friction
    shape.elasticity = self.elasticity
    shape.mass = self.mass
    shape.collision_type = coll
    return shape

  def add_sensors(self, coll):
    sensor = pymunk.Circle(self.body, self.radius + self.melee_range)
    sensor.sensor = True
    sensor.collision_type = coll
    return sensor
  
  def update(self, dt): 
    return 
  
  
  def draw(self, DEBUG):
    pos = to_pygame(self.body.position, self.game.screen)
    pygame.draw.circle(self.game.screen, self.col, pos, self.radius)

    if DEBUG:
      # if self.unit.is_selected:
      #   pygame.draw.circle(self.game.screen, BLACK, pos, self.radius - 1)
  
      # DRAW ARROW TO TARGET
      p1 = to_pygame(self.body.position, self.game.screen)
      p2 = to_pygame(self.target_position, self.game.screen)
      pygame.draw.aalines(self.game.screen, BLUE, False, [p1,p2])
  
      # DRAW VELOCITY
      # p1 = to_pygame(self.body.position, self.game.screen)
      # p2 = to_pygame(self.body.position + self.body.velocity.normalized() * 100, self.game.screen)
      # pygame.draw.aalines(self.game.screen, BLUE, False, [p1,p2])
  
      # DRAW COORDINATES 
      # draw_text(f"{self.coord[0]},{self.coord[1]}", self.game.screen, self.game.font, self.body.position,
      #           np.pi)
      pass