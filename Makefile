.PHONY: docs docs-serve

# Build the static documentation site into build/docs (GitHub Pages upload folder).
docs:
	rm -rf build/docs build/api
	dotnet tool restore
	dotnet tool run docfx docfx.json

# Serve the generated static site locally (recommended vs file:// previews).
docs-serve: docs
	cd build/docs && python3 -m http.server 8080
