import numpy as np
import pymunk

from pymunk.vec2d import Vec2d

def vector_clipping(v, min_length, max_length):
    if v.length > max_length:
        return v.normalized() * max_length
    elif v.length < min_length:
        return v.normalized() * min_length        
    return v

def vect_linspace(v1, v2, n):
    prs = np.linspace(0,1, n+1).tolist()            
    res = []
    for i in range(n):
        pr_start = prs[i]
        pr_end = prs[i+1]
        cur_start = v1*(pr_start) + (v2)*(1-pr_start)
        cur_end = v1*(pr_end) + (v2)*(1-pr_end)
        res.append((cur_start, cur_end))
    return res
        
def vect_middles_of_linspace(v1,v2,n):
    divs = np.linspace(0,1, n+2).tolist()[1:-1]
    return np.array([(v1 * div + v2 * (1 - div)).int_tuple for div in divs])


def get_BB(v1, v2):
    v1,v2 = Vec2d(v1), Vec2d(v2)
    left = min(v1.x, v2.x) #+ abs(v1.x - v2.x) / 2
    bot  = min(v1.y, v2.y) #- abs(v1.y - v2.y) / 2
    right = max(v1.x, v2.x) #+ abs(v1.x - v2.x) / 2
    top  = max(v1.y, v2.y) #- abs(v1.y - v2.y) / 2
    return pymunk.BB(left, bot, right, top)