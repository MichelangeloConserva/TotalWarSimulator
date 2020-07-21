import pygame

from pymunk.pygame_util import to_pygame, from_pygame

BLACK = (0, 0, 0)

def draw_text(t, screen, font, pos, angle):
    pos = to_pygame(pos, screen)
    text = font.render(t, True, BLACK)
    text = pygame.transform.rotate(text, angle)
    text_rect = text.get_rect(center = pos)
    screen.blit(text, text_rect)