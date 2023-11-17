# project/Makefile

# List of sub-modules

.PHONY: all rebuild raylib wisor clean

MODULES = all raylib/src wisor

all:
	@for module in $(MODULES); do \
		$(MAKE) -C $$module $(MAKEFLAGS); \
	done

rebuild:
	@if [ -z "$(module)" ]; then \
		for module in $(MODULES); do \
			$(MAKE) -C $$module clean;  \
			$(MAKE) -C $$module $(MAKEFLAGS); \
 		done; \
	else \
		$(MAKE) -C $(module) clean;  \
		$(MAKE) -C $(module) $(MAKEFLAGS); \
	fi

raylib:
	$(MAKE) -C raylib/src $(MAKEFLAGS);

wisor:
	$(MAKE) -C wisor $(MAKEFLAGS);

clean:
	@if [ -z "$(module)" ]; then \
		for module in $(MODULES); do \
			$(MAKE) -C $$module clean; \
		done; \
	else \
		$(MAKE) -C $(module) clean; \
	fi