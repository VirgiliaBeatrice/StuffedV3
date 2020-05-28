#include <xc.h>
#include "spiPwmDefs.h"
    .globl    __vector_dispatch_38	#SPI2TX empty
    .section    .vector_38,code,keep
    .set nomips16
    .set micromips
    .set noreorder
    .align  2
    .ent    __vector_dispatch_38
__vector_dispatch_38:
    j    _spiEmpty
    nop
    .set reorder
    .end    __vector_dispatch_38
    .size    __vector_dispatch_38, .-__vector_dispatch_38

    .section .bss
    .align 4
    .global addrSPI2BUF
    .global spiPwmWord1
    .global spiPwmWord2
#ifdef SPIPWM128
    .global spiPwmWord3
    .global spiPwmWord4
#endif
    .global spiPwmGpBackup
    spiPwmBase:
    addrSPI2BUF:	.space  4
    spiPwmWord1:	.space	4
    spiPwmWord2:	.space	4
#ifdef SPIPWM128
    spiPwmWord3:	.space	4
    spiPwmWord4:	.space	4
#endif
    spiPwmGpBackup:	.space  4
    spiPwmV0Backup:	.space  4    
    .text
    .set nomips16
    .set micromips
    .set noreorder
    .align 2
    .global _spiEmpty
    .ent    _spiEmpty
_spiEmpty:
    la $k1, spiPwmBase			    	# load base address to $k1
    ori $k1, $k1, spiPwmBase			# lower bits
    sw $gp, (spiPwmGpBackup-spiPwmBase)($k1)	# backup $gp
    sw $v0, (spiPwmV0Backup-spiPwmBase)($k1)	# backup $v0    
    lw $gp, (addrSPI2BUF-spiPwmBase)($k1)	# set $gp to SPI2BUF address 
    # load pwm pattern.
    lw $k0, (spiPwmWord1-spiPwmBase)($k1)	# set $k0 to spiPwmWord1
    lw $v0, (spiPwmWord2-spiPwmBase)($k1)	# set $v0 to spiPwmWord2
    # write pwm pattern to SPI2BUF.
    sw $k0, 0($gp)				
    sw $v0, 0($gp)				
#ifdef SPIPWM128
    lw $k0, (spiPwmWord3-spiPwmBase)($k1)	# set $k0 to spiPwmWord1
    lw $v0, (spiPwmWord4-spiPwmBase)($k1)	# set $v0 to spiPwmWord2
#endif
    sw $k0, 0($gp)
    sw $v0, 0($gp)
    li $v0, 0x40				# clear IFS
    sw $v0, 0x6F34($gp)				# 0x6F34 = IFS1CLR from $gp=SPI2BUF
						# SPI2BUF=0xBF808120, IFS1CLRADR=0xBF80F054 
						# see C:\Program Files (x86)\Microchip\xc32\v2.10\pic32mx\include\procp32mm0064gpm036.h

    # restore
    lw $gp, (spiPwmGpBackup-spiPwmBase)($k1)	# load backed up $gp
    lw $v0, (spiPwmV0Backup-spiPwmBase)($k1)	# load backed up $v0
    eret
    jalx
    nop
    .set reorder
    .end    _spiEmpty
    .size    _spiEmpty, .-_spiEmpty
