

def begin_solve_ally(arbiter, space, _):
    # s1 starts the collision
    s1, s2 = arbiter.shapes
    
    # if  not s1.sensor and not s2.sensor:
    #     spring_to_mantain(s1, s2.body.position)
    #     spring_to_mantain(s2, s2.body.position)
    
    # if not s1.sensor and not s2.sensor: return False
    # if s1.sensor and not s2.sensor:
    #     print("entering")
    #     s1.body.soldier.col = GREEN
    # elif s2.sensor and not s1.sensor:
    #     print("entering")
    
    return True

def separate_solve_ally(arbiter, space, _):
    # s1 starts the collision
    s1, s2 = arbiter.shapes
    
    # if  not s1.sensor and not s2.sensor:
    #     spring_to_mantain(s1, s2.body.position)
    #     spring_to_mantain(s2, s2.body.position)
    
    # if s1.sensor and not s2.sensor:
    #     print("exting")
    #     s1.body.soldier.col = RED
    # elif s2.sensor and not s1.sensor:
    #     print("exting")
    
    return True

def begin_solve_enemy(arbiter, space, _):
    # s1 starts the collision
    s1, s2 = arbiter.shapes
    
    # if not s1.sensor and not s2.sensor:
    #     print("impulse")
    #     f = s1.mass * s1.body.velocity
    #     s2.body.apply_impulse_at_world_point(f, s2.body.position)
    
    return True

def separate_solve_enemy(arbiter, space, _):
    # s1 starts the collision
    s1, s2 = arbiter.shapes
    
    # if  not s1.sensor and not s2.sensor:
    #     spring_to_mantain(s1, s2.body.position, s1.body.soldier.game.space)
    #     spring_to_mantain(s2, s2.body.position, s1.body.soldier.game.space)
    
    return True