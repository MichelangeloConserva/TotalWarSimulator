import pygame

from pymunk.pygame_util import to_pygame, DrawOptions
from pygame.locals import QUIT, KEYDOWN, K_ESCAPE, K_q, K_SPACE

GRAY = (220, 220, 220)
BLACK = (0, 0, 0)
WHITE = (255, 255, 255)
RED = (255, 0, 0)
GREEN = (0, 255, 0)


fps = 60
WIDTH, HEIGHT = 700, 700

class App:
    def __init__(self, space):
        pygame.init()
        self.clock = pygame.time.Clock()
        self.screen = pygame.display.set_mode((WIDTH, HEIGHT))
        self.draw_options = DrawOptions(self.screen)
        self.running = True
        self.space = space
        self.images = []

    def do_event(self, event):
        if event.type == QUIT:
            self.running = False

        if event.type == KEYDOWN:
            if event.key in (K_q, K_ESCAPE):
                self.running = False

    def draw(self):
        self.screen.fill(GRAY)
        self.space.debug_draw(self.draw_options)
        pygame.display.update()

        text = f'fpg: {self.clock.get_fps():.1f}'
        pygame.display.set_caption(text)