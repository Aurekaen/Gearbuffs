# Gearbuffs
Applies buffs to players if they have a specified item.
Also allows for Auras and Antiauras. 
Auras apply GearBuffs to all teammates (and the player who has the item)within a designated radius, while AntiAuras apply to all players on a different team(and DO NOT apply to the person holding the item).
USAGE:
/gb add [item] [buff] [duration] [held]
/gb aura [item] [buff] [duration] [range] [held]
/gb antiaura [item] [buff] [duration] [range] [held]
/gb del [item]
  Removes the first gearbuff added to the item

Parameter Explanation:
[item]:the item name or ID that you want the GearBuff to be tied to
[buff]:the buff name or ID you want anyone holding [item] to recieve
[duration]:the duration of the buff, in seconds, as an integer. Affects how long it lasts after removing the item. Also affects the effectiveness of Mana Sickness
[held]:Whether you want the item to trigger GearBuffs at any point when it is in the player's inventory, or only when it is in the player's active slot. If this parameter is "true", the GearBuff will apply only when the item is held, all other parameters will result in it triggering if the item is present in the player's inventory.
[range]:the range in blocks that a player must be in to be given an aura or antiaura
