

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
    s1, s2 = arbiter.shapes
    
    if   s1.sensor and not s2.sensor: s1.body.soldier.enemy_melee_range.add(s2)
    elif s2.sensor and not s1.sensor: s2.body.soldier.enemy_melee_range.add(s1)
    
    return True

def separate_solve_enemy(arbiter, space, _):
    s1, s2 = arbiter.shapes
    
    if   s1.sensor and not s2.sensor: s1.body.soldier.enemy_melee_range.remove(s2)
    elif s2.sensor and not s1.sensor: s2.body.soldier.enemy_melee_range.remove(s1)
    
    return True

def begin_solve_utils(arbiter, space, _):
    return False




































