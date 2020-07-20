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
        
def limit_velocity(body, gravity, damping, dt):
    pymunk.Body.update_velocity(body, gravity, damping, dt)
    l = body.velocity.length
    if l > body.soldier.max_speed:
        scale = body.soldier.max_speed / l
        body.velocity = body.velocity * scale





























