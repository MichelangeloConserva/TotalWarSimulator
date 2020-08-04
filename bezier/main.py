import pygame
import os
import sys
import control
import bezier


def main():
    os.environ['SDL_VIDEO_CENTERED'] = '1'
    pygame.init()
    pygame.mixer.init()

    SCREEN_SIZE = (720, 480)
    DESIRED_FPS = 60.0

    pygame.display.set_mode(SCREEN_SIZE)
    pygame.display.set_caption("Bezier")

    state_dict = {
    			   # Insert your states here
    			    "main_state" : bezier.MainState()
                  }
    
    game_control = control.Control(1000.0 / DESIRED_FPS)
    # Change and uncomment line below, after you complete state_dict
    game_control.setup_states(state_dict, "main_state")
    game_control.main()
    pygame.quit()
    sys.exit()

if __name__ == "__main__":
    main()
