import pymunk

def create_space(WIDTH, HEIGHT):
    space = pymunk.Space()
    space.iterations = 10
    space.sleep_time_threshold = 0.5
    
    static_body = space.static_body
        
    # Create segments around the edge of the screen.
    shape = pymunk.Segment(static_body, (1,1), (1,HEIGHT), 1.0)
    space.add(shape)
    shape.elasticity = 1
    shape.friction = 1

    shape = pymunk.Segment(static_body, (WIDTH-2,1), (WIDTH-2,HEIGHT), 1.0)
    space.add(shape)
    shape.elasticity = 1
    shape.friction = 1
    
    shape = pymunk.Segment(static_body, (1,1+1), (WIDTH,1+1), 1.0)
    space.add(shape)
    shape.elasticity = 1
    shape.friction = 1
    
    shape = pymunk.Segment(static_body, (1,HEIGHT), (WIDTH,HEIGHT), 1.0)
    space.add(shape)
    shape.elasticity = 1
    shape.friction = 1
    
    return space


def is_in_selection(pos, start_pos, end_pos):
    return min(start_pos[0], end_pos[0]) < pos[0] and pos[0] < max(start_pos[0], end_pos[0]) and\
           min(start_pos[1], end_pos[1]) < pos[1] and pos[1] < max(start_pos[1], end_pos[1])\


def is_on_enemy_unity(pos, army):
    for u in army.enemy.units:
        cc = u.corners
        if is_in_selection(pos, cc[0], cc[1]): return u
    return None