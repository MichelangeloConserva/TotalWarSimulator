

def begin_solve_ally(arbiter, space, _):
    # s1 starts the collision
    s1, s2 = arbiter.shapes
    return True

def separate_solve_ally(arbiter, space, _):
    # s1 starts the collision
    s1, s2 = arbiter.shapes
    return True

def begin_solve_enemy(arbiter, space, _):
    s1, s2 = arbiter.shapes
    
    if   s1.sensor and not s2.sensor: s1.body.soldier.enemy_melee_range.add(s2.body.soldier)
    elif s2.sensor and not s1.sensor: s2.body.soldier.enemy_melee_range.add(s1.body.soldier)
    
    return True

def separate_solve_enemy(arbiter, space, _):
    s1, s2 = arbiter.shapes
    
    if   s1.sensor and not s2.sensor: s1.body.soldier.enemy_melee_range.remove(s2.body.soldier)
    elif s2.sensor and not s1.sensor: s2.body.soldier.enemy_melee_range.remove(s1.body.soldier)
    
    return True

def begin_solve_utils(arbiter, space, _):
    return False




































