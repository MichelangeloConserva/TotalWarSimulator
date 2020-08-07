import pygame
import numpy as np

from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_q, MOUSEBUTTONDOWN, MOUSEBUTTONUP
from pymunk.pygame_util import DrawOptions

from utils.settings.physics import space_damping
from utils.settings.game import WIDTH, HEIGHT
from utils.pymunk_utils import create_space, add_collisions

class Game:

  def __init__(self, record = True):

    self.armies  = []
    self.objects = []
    self.video   = []
    self.fps     = 60.0
    self.done    = False

    self.record = record

    self.create_screen()
    self.create_space()

    self.draw_options = DrawOptions(self.screen)

# =============================================================================
# Instantiation
# =============================================================================

  def create_screen(self):
    pygame.init()
    self.font = pygame.font.Font(pygame.font.get_default_font(), 8)
    self.screen = pygame.display.set_mode((WIDTH,HEIGHT))
    self.clock = pygame.time.Clock()

  def create_space(self):
    self.space = create_space(WIDTH, HEIGHT)
    self.space.damping = space_damping
    add_collisions(self.space)


# =============================================================================
# Called at every frame
# =============================================================================

  def update(self, dt):
    self.space.step(dt)
    for army in self.armies: army.update(dt)

  def draw(self, DEBUG):

    # Pymunk debugging draw (constraints and stuff). N.B. Quite slow
    if DEBUG: self.space.debug_draw(self.draw_options)

    # Drawing soldiers
    for a in self.armies: a.draw(DEBUG)

    # Other stuff
    self.show_fps()

# =============================================================================
# Utilities
# =============================================================================

  def save_video(self):
    from pygifsicle import optimize
    import imageio
    imageio.mimwrite('test_.gif', self.video , fps = self.fps)
    optimize("test_.gif")

  def show_fps(self):
    fps = self.font.render(str(int(self.clock.get_fps())), True, pygame.Color('black'))
    self.screen.blit(fps, (50, 50))