import pygame


class State():
    def __init__(self):
        # done will cause the program to move onto the next state
        self.done = False
        self.next = None
        # quit causes whole program to quit
        self.quit = False
        self.previous = None
        
    def startup(self):
        pass
		
    def cleanup(self):
        pass
		
    def get_event(self, event):
        pass
		
    def update(self, screen):
        pass

class Control():
    def __init__(self, ms_per_update):
        self.MS_PER_UPDATE = ms_per_update
        self.screen = pygame.display.get_surface()
        self.game_done = False
        self.CLOCK = pygame.time.Clock()

        self.state_dict = None
        self.state_name = None
        self.state = None

    def setup_states(self, state_dict, start_state):
        self.state_dict = state_dict
        self.state_name = start_state
        self.state = self.state_dict[self.state_name]
        self.state.startup()

    def flip_state(self):
        self.state.done = False
        previous, self.state_name = self.state_name, self.state.next
        self.state.cleanup()
        self.state = self.state_dict[self.state_name]
        self.state.startup()
        self.state.previous = previous

    def update(self):
        if self.state.quit:
            self.game_done = True
        elif self.state.done:
            self.flip_state()
        self.state.update(self.screen)

    def event_loop(self):
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                self.game_done = True
            self.state.get_event(event)

    def main(self):
        lag = 0.0
        while not self.game_done:
            lag += self.CLOCK.tick()

            self.event_loop()
            while lag >= self.MS_PER_UPDATE:
                self.update()
                lag -= self.MS_PER_UPDATE

            pygame.display.update()
