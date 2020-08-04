import pygame


class Text():
    def __init__(self, text, size, color, pos):
        self.font = pygame.font.Font(None, size)
        self.color = color
        self.text = self.font.render(text, 1, color)
        self.pos = pos

    def update(self, text_to_render):
        self.text = self.font.render(text_to_render, 1, self.color)

    def draw(self, surface):
        surface.blit(self.text, self.pos)