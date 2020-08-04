import pygame
from . import control
from . import text
from . import colors


def interpolate(v1, v2, t):
    x = (1-t) * v1.x + t * v2.x
    y = (1-t) * v1.y + t * v2.y
    return pygame.math.Vector2(x, y)

class MainState(control.State):
    def __init__(self):
        control.State.__init__(self)
        self.SCREEN_SIZE = pygame.display.get_surface().get_size()
        self.key_bindings_dict = {"LEFT": pygame.K_a,
                                 "RIGHT": pygame.K_d,
                                 "UP": pygame.K_w,
                                 "DOWN": pygame.K_s,
                                 }
        self.direction_dict = {"LEFT": pygame.math.Vector2(-1, 0),
                           "RIGHT": pygame.math.Vector2(1, 0),
                           "UP": pygame.math.Vector2(0, -1),
                           "DOWN": pygame.math.Vector2(0, 1)
                           }
        self.pressed_keys = pygame.key.get_pressed()
        self.vertex_control = 0
        self.control_vertex_list = []
        self.curve_vertex_list = []
        self.speed = 5
        self.step = 30
        self.width = 3
        self.step_text = text.Text(str(self.step), 30, colors.BLACK, (30, 30))
        self.timer = 0

    def get_event(self, event):
        if event.type == pygame.MOUSEBUTTONDOWN:
            self.control_vertex_list.append(pygame.math.Vector2(*pygame.mouse.get_pos()))
        if event.type in (pygame.KEYDOWN, pygame.KEYUP):
            self.pressed_keys = pygame.key.get_pressed()
            if self.pressed_keys[pygame.K_ESCAPE]:
                self.quit = True
        if event.type == pygame.KEYDOWN and event.key == pygame.K_TAB:
            self.change_control()
        if event.type == pygame.KEYDOWN and event.key == pygame.K_SPACE:
            self.curve_vertex_list.clear()
            self.make_curve_points()

    def update(self, screen):
        for key in self.key_bindings_dict:
                    if self.pressed_keys[self.key_bindings_dict[key]] and len(self.control_vertex_list) > 0:
                        self.control_vertex_list[self.vertex_control] += self.direction_dict[key] * self.speed            
        self.update_step()
        self.step_text.update(str(self.step))
        # Draw
        screen.fill(colors.WHITE)
        self.step_text.draw(screen)
        for i, vertex in enumerate(self.control_vertex_list):
            color = colors.RED if i == self.vertex_control else colors.BLUE
            pygame.draw.circle(screen, color, (int(vertex.x), int(vertex.y)), 7)

        if len(self.curve_vertex_list) > 1:
            pygame.draw.lines(screen, colors.GREEN, False, self.curve_vertex_list, self.width)
            # Draw last line from last curve point to last control point
            pygame.draw.line(screen, colors.GREEN, self.curve_vertex_list[-1], self.control_vertex_list[-1], self.width)

    def update_step(self):
        if self.pressed_keys[pygame.K_q]:
            self.timer += 1
            if self.timer > 10:
                self.step += 1
                self.timer = 0

        elif self.pressed_keys[pygame.K_e]:
            self.timer += 1
            if self.timer > 10:
                self.step -= 1
                self.timer = 0

    def change_control(self):
        self.vertex_control += 1
        if self.vertex_control > len(self.control_vertex_list)-1:
            self.vertex_control = 0

    def make_curve_points(self):
        for i in range(self.step):
            t = i / self.step
            point = self.get_point(len(self.control_vertex_list) - 1, 0, t)
            self.curve_vertex_list.append(point)

    def get_point(self, r, i, t):
        if r == 0:
            return self.control_vertex_list[i]
        p1 = self.get_point(r - 1, i, t)
        p2 = self.get_point(r - 1, i + 1, t)
        return interpolate(p1, p2, t)
